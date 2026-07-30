using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TurkcellMeetingAssistant.Application.Common.Exceptions;
using TurkcellMeetingAssistant.Application.Common.Models;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Email;
using TurkcellMeetingAssistant.Application.Interfaces;
using TurkcellMeetingAssistant.Domain.Entities;
using TurkcellMeetingAssistant.Domain.Enums;

namespace TurkcellMeetingAssistant.Application.Services;

public interface IMeetingEmailAppService
{
    Task<EmailPreviewDto> PreviewEmailAsync(Guid meetingId, GenerateEmailPreviewRequest request, CancellationToken cancellationToken);
    Task<EmailLogDto> SendMeetingEmailAsync(Guid meetingId, SendMeetingEmailRequest request, CancellationToken cancellationToken);
    Task<EmailLogDto> SendTestEmailAsync(Guid meetingId, SendTestEmailRequest request, CancellationToken cancellationToken);
    Task<List<EmailLogDto>> GetEmailLogsAsync(Guid meetingId, CancellationToken cancellationToken);
    Task<EmailStatusDto> GetEmailStatusAsync(Guid meetingId, CancellationToken cancellationToken);
    Task<EmailLogDetailDto> GetEmailLogDetailAsync(Guid meetingId, Guid emailLogId, CancellationToken cancellationToken);
}

public class MeetingEmailAppService : IMeetingEmailAppService
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IMeetingEmailTemplateService _templateService;
    private readonly ICurrentUserService _currentUserService;
    private readonly EmailSettings _settings;

    public MeetingEmailAppService(
        IApplicationDbContext context,
        IEmailService emailService,
        IMeetingEmailTemplateService templateService,
        ICurrentUserService currentUserService,
        IOptions<EmailSettings> options)
    {
        _context = context;
        _emailService = emailService;
        _templateService = templateService;
        _currentUserService = currentUserService;
        _settings = options.Value;
    }

    private async Task<(Meeting meeting, MeetingSummary summary, List<ActionItem> actions, List<MeetingTopic> topics, List<MeetingDecision> decisions, List<OpenIssue> issues, List<MeetingParticipant> participants)> GetMeetingDataAsync(Guid meetingId, bool requireApproved, CancellationToken cancellationToken)
    {
        var meeting = await _context.Meetings
            .Include(m => m.OrganizerUser)
            .FirstOrDefaultAsync(m => m.Id == meetingId && !m.IsDeleted, cancellationToken);
            
        if (meeting == null) throw new NotFoundException(nameof(Meeting), meetingId);

        // Authorization (Admin or Organizer)
        if (_currentUserService.Role != UserRole.Admin.ToString() && meeting.OrganizerUserId != _currentUserService.UserId)
            throw new ForbiddenAccessException();

        var summary = await _context.MeetingSummaries
            .Where(s => s.MeetingId == meetingId && !s.IsDeleted)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (summary == null) throw new BadRequestException("Toplantı özeti bulunamadı.");

        if (requireApproved && (!summary.IsApproved || meeting.Status != MeetingStatus.Approved))
            throw new BadRequestException("Özet henüz onaylanmamış veya onayı düşmüş.");

        var actions = await _context.ActionItems.Where(a => a.MeetingSummaryId == summary.Id && !a.IsDeleted).ToListAsync(cancellationToken);
        var topics = await _context.MeetingTopics.Where(t => t.MeetingSummaryId == summary.Id && !t.IsDeleted).ToListAsync(cancellationToken);
        var decisions = await _context.MeetingDecisions.Where(d => d.MeetingSummaryId == summary.Id && !d.IsDeleted).ToListAsync(cancellationToken);
        var issues = await _context.OpenIssues.Where(i => i.MeetingSummaryId == summary.Id && !i.IsDeleted).ToListAsync(cancellationToken);
        var participants = await _context.MeetingParticipants.Where(p => p.MeetingId == meetingId).ToListAsync(cancellationToken);

        return (meeting, summary, actions, topics, decisions, issues, participants);
    }

    private void ValidateEmailAddress(string email)
    {
        if (_settings.AllowExternalRecipients) return;
        
        var domain = email.Split('@').LastOrDefault()?.ToLower();
        if (domain == null || !_settings.AllowedEmailDomains.Contains(domain))
        {
            throw new BadRequestException($"Harici e-posta adreslerine gönderim engellenmiştir. Yasaklı alan adı: {domain}");
        }
    }

    public async Task<EmailPreviewDto> PreviewEmailAsync(Guid meetingId, GenerateEmailPreviewRequest request, CancellationToken cancellationToken)
    {
        var data = await GetMeetingDataAsync(meetingId, true, cancellationToken);

        var message = await _templateService.GeneratePreviewEmailAsync(
            data.meeting, data.summary, data.actions, data.topics, data.decisions, data.issues, data.participants,
            request.SubjectOverride, request.IntroText, request.ClosingText,
            request.IncludeParticipants, request.IncludeTopics, request.IncludeDecisions, 
            request.IncludeActionItems, request.IncludeOpenIssues, request.IncludeActionStatus, 
            request.IncludeEvidence, cancellationToken);

        return new EmailPreviewDto
        {
            Subject = message.Subject,
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody
        };
    }

    public async Task<EmailLogDto> SendMeetingEmailAsync(Guid meetingId, SendMeetingEmailRequest request, CancellationToken cancellationToken)
    {
        // Check idempotency
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await _context.EmailLogs
                .FirstOrDefaultAsync(e => e.IdempotencyKey == request.IdempotencyKey, cancellationToken);
                
            if (existing != null)
            {
                if (existing.Status == EmailDeliveryStatus.Pending)
                    throw new ConcurrencyException("Bu e-posta zaten gönderiliyor.");
                    
                return MapToDto(existing); // Return existing successfully sent/failed result
            }
        }
        else
        {
            request.IdempotencyKey = Guid.NewGuid().ToString(); // if not provided
        }

        var data = await GetMeetingDataAsync(meetingId, true, cancellationToken);

        var message = await _templateService.GeneratePreviewEmailAsync(
            data.meeting, data.summary, data.actions, data.topics, data.decisions, data.issues, data.participants,
            request.SubjectOverride, request.IntroText, request.ClosingText,
            request.IncludeParticipants, request.IncludeTopics, request.IncludeDecisions, 
            request.IncludeActionItems, request.IncludeOpenIssues, request.IncludeActionStatus, 
            request.IncludeEvidence, cancellationToken);

        // Configure Recipients
        message.From = new EmailAddressModel { Email = _settings.DefaultSenderEmail, Name = _settings.DefaultSenderName };
        message.IdempotencyKey = request.IdempotencyKey;
        
        var toEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var p in data.participants)
        {
            ValidateEmailAddress(p.Email);
            toEmails.Add(p.Email);
            message.To.Add(new EmailAddressModel { Email = p.Email, Name = p.FullName });
        }
        
        if (request.AdditionalTo != null)
        {
            foreach(var a in request.AdditionalTo)
            {
                if (toEmails.Add(a.Email))
                {
                    ValidateEmailAddress(a.Email);
                    message.To.Add(new EmailAddressModel { Email = a.Email, Name = a.Name });
                }
            }
        }

        if (request.Cc != null)
        {
            foreach(var c in request.Cc)
            {
                if (!toEmails.Contains(c.Email))
                {
                    ValidateEmailAddress(c.Email);
                    message.Cc.Add(new EmailAddressModel { Email = c.Email, Name = c.Name });
                }
            }
        }
        
        if (message.To.Count + message.Cc.Count > _settings.MaximumRecipientCount)
            throw new BadRequestException($"Maximum alıcı sayısını ({_settings.MaximumRecipientCount}) aştınız.");

        // Create DB Log
        var emailLog = new EmailLog
        {
            Id = Guid.NewGuid(),
            MeetingId = meetingId,
            MeetingSummaryId = data.summary.Id,
            SummaryVersion = data.summary.Version,
            SummaryManualRevisionNumber = data.summary.ManualRevisionNumber,
            EmailType = EmailType.MeetingSummary,
            Provider = _settings.Provider,
            SenderEmail = _settings.DefaultSenderEmail,
            ToRecipientsJson = JsonSerializer.Serialize(message.To.Select(t => t.Email)),
            CcRecipientsJson = JsonSerializer.Serialize(message.Cc.Select(c => c.Email)),
            Subject = message.Subject,
            BodyHtml = _settings.StoreEmailBody ? message.HtmlBody : null,
            BodyText = _settings.StoreEmailBody ? message.TextBody : null,
            Status = EmailDeliveryStatus.Pending,
            IsTestEmail = false,
            RequestedByUserId = _currentUserService.UserId ?? Guid.Empty,
            RequestedAt = DateTime.UtcNow,
            IdempotencyKey = request.IdempotencyKey,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmailLogs.Add(emailLog);
        await _context.SaveChangesAsync(cancellationToken);

        // Send Email via Service
        try
        {
            var result = await _emailService.SendAsync(message, cancellationToken);
            
            emailLog.Status = result.IsSuccessful ? EmailDeliveryStatus.Sent : EmailDeliveryStatus.Failed;
            emailLog.ProviderMessageId = result.ProviderMessageId;
            emailLog.ErrorCode = result.ErrorCode;
            emailLog.ErrorMessage = result.ErrorMessage;
            
            if (result.IsSuccessful)
            {
                emailLog.SentAt = result.SentAt;
                
                // Update Meeting status if it was just Approved
                // Phase 6 Requirement: E-posta gönderildiğinde status güncellemesi yapılabilir.
                if (data.meeting.Status == MeetingStatus.Approved)
                {
                    data.meeting.Status = MeetingStatus.EmailSent;
                }
            }
            else
            {
                emailLog.FailedAt = DateTime.UtcNow;
            }
        }
        catch (Exception ex)
        {
            emailLog.Status = EmailDeliveryStatus.Failed;
            emailLog.ErrorCode = "SYSTEM_ERROR";
            emailLog.ErrorMessage = ex.Message;
            emailLog.FailedAt = DateTime.UtcNow;
        }

        emailLog.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(emailLog);
    }

    public async Task<EmailLogDto> SendTestEmailAsync(Guid meetingId, SendTestEmailRequest request, CancellationToken cancellationToken)
    {
        if (!_settings.EnableTestEmail) throw new BadRequestException("Test e-posta gönderimi sistem ayarlarından kapatılmıştır.");
        
        ValidateEmailAddress(request.ToEmail);
        
        var data = await GetMeetingDataAsync(meetingId, true, cancellationToken);

        var message = await _templateService.GeneratePreviewEmailAsync(
            data.meeting, data.summary, data.actions, data.topics, data.decisions, data.issues, data.participants,
            null, null, null, true, true, true, true, true, true, false, cancellationToken);
            
        message.Subject = $"[TEST] {message.Subject}";
        message.From = new EmailAddressModel { Email = _settings.DefaultSenderEmail, Name = _settings.DefaultSenderName };
        message.To.Add(new EmailAddressModel { Email = request.ToEmail });
        message.IdempotencyKey = Guid.NewGuid().ToString();

        var emailLog = new EmailLog
        {
            Id = Guid.NewGuid(),
            MeetingId = meetingId,
            MeetingSummaryId = data.summary.Id,
            SummaryVersion = data.summary.Version,
            SummaryManualRevisionNumber = data.summary.ManualRevisionNumber,
            EmailType = EmailType.Test,
            Provider = _settings.Provider,
            SenderEmail = _settings.DefaultSenderEmail,
            ToRecipientsJson = JsonSerializer.Serialize(new[] { request.ToEmail }),
            Subject = message.Subject,
            BodyHtml = _settings.StoreEmailBody ? message.HtmlBody : null,
            BodyText = _settings.StoreEmailBody ? message.TextBody : null,
            Status = EmailDeliveryStatus.Pending,
            IsTestEmail = true,
            RequestedByUserId = _currentUserService.UserId ?? Guid.Empty,
            RequestedAt = DateTime.UtcNow,
            IdempotencyKey = message.IdempotencyKey,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.EmailLogs.Add(emailLog);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            var result = await _emailService.SendAsync(message, cancellationToken);
            
            emailLog.Status = result.IsSuccessful ? EmailDeliveryStatus.Sent : EmailDeliveryStatus.Failed;
            emailLog.ProviderMessageId = result.ProviderMessageId;
            emailLog.ErrorCode = result.ErrorCode;
            emailLog.ErrorMessage = result.ErrorMessage;
            
            if (result.IsSuccessful) emailLog.SentAt = result.SentAt;
            else emailLog.FailedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            emailLog.Status = EmailDeliveryStatus.Failed;
            emailLog.ErrorCode = "SYSTEM_ERROR";
            emailLog.ErrorMessage = ex.Message;
            emailLog.FailedAt = DateTime.UtcNow;
        }

        emailLog.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(emailLog);
    }

    public async Task<List<EmailLogDto>> GetEmailLogsAsync(Guid meetingId, CancellationToken cancellationToken)
    {
        var meeting = await _context.Meetings.FirstOrDefaultAsync(m => m.Id == meetingId && !m.IsDeleted, cancellationToken);
        if (meeting == null) throw new NotFoundException(nameof(Meeting), meetingId);

        if (_currentUserService.Role != UserRole.Admin.ToString() && meeting.OrganizerUserId != _currentUserService.UserId)
            throw new ForbiddenAccessException();

        var logs = await _context.EmailLogs
            .AsNoTracking()
            .Where(e => e.MeetingId == meetingId)
            .OrderByDescending(e => e.RequestedAt)
            .ToListAsync(cancellationToken);
            
        return logs.Select(MapToDto).ToList();
    }

    private EmailLogDto MapToDto(EmailLog log)
    {
        return new EmailLogDto
        {
            Id = log.Id,
            MeetingId = log.MeetingId,
            EmailType = log.EmailType,
            Status = log.Status,
            Subject = log.Subject,
            ToRecipientsJson = log.ToRecipientsJson,
            CcRecipientsJson = log.CcRecipientsJson,
            RequestedAt = log.RequestedAt,
            SentAt = log.SentAt,
            ErrorMessage = log.ErrorMessage
        };
    }

    public async Task<EmailStatusDto> GetEmailStatusAsync(Guid meetingId, CancellationToken cancellationToken)
    {
        var meeting = await _context.Meetings.FirstOrDefaultAsync(m => m.Id == meetingId && !m.IsDeleted, cancellationToken);
        if (meeting == null) throw new NotFoundException(nameof(Meeting), meetingId);

        if (_currentUserService.Role != UserRole.Admin.ToString() && meeting.OrganizerUserId != _currentUserService.UserId)
            throw new ForbiddenAccessException();

        var summary = await _context.MeetingSummaries
            .Where(s => s.MeetingId == meetingId && !s.IsDeleted)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(cancellationToken);

        var lastLog = await _context.EmailLogs
            .AsNoTracking()
            .Where(e => e.MeetingId == meetingId && e.EmailType == EmailType.MeetingSummary)
            .OrderByDescending(e => e.RequestedAt)
            .Select(e => new { e.Status, e.SentAt, e.ErrorMessage })
            .FirstOrDefaultAsync(cancellationToken);

        var isSummaryApproved = summary != null && summary.IsApproved;
        var isApprovalStillValid = isSummaryApproved && meeting.Status == MeetingStatus.Approved;
        var hasSentEmail = lastLog != null && lastLog.Status == EmailDeliveryStatus.Sent;

        var dto = new EmailStatusDto
        {
            MeetingId = meetingId,
            MeetingStatus = meeting.Status,
            IsSummaryApproved = isSummaryApproved,
            IsApprovalStillValid = isApprovalStillValid,
            HasSentEmail = hasSentEmail,
            LastEmailStatus = lastLog?.Status,
            LastEmailSentAt = lastLog?.SentAt,
            LastEmailError = lastLog?.ErrorMessage,
            CanPreview = isSummaryApproved,
            CanSendTestEmail = _settings.EnableTestEmail && isSummaryApproved,
            CanSend = isApprovalStillValid && !hasSentEmail
        };

        return dto;
    }

    public async Task<EmailLogDetailDto> GetEmailLogDetailAsync(Guid meetingId, Guid emailLogId, CancellationToken cancellationToken)
    {
        var meeting = await _context.Meetings.FirstOrDefaultAsync(m => m.Id == meetingId && !m.IsDeleted, cancellationToken);
        if (meeting == null) throw new NotFoundException(nameof(Meeting), meetingId);

        if (_currentUserService.Role != UserRole.Admin.ToString() && meeting.OrganizerUserId != _currentUserService.UserId)
            throw new ForbiddenAccessException();

        var log = await _context.EmailLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == emailLogId, cancellationToken);

        if (log == null || log.MeetingId != meetingId)
            throw new NotFoundException(nameof(EmailLog), emailLogId);

        return new EmailLogDetailDto
        {
            Id = log.Id,
            MeetingId = log.MeetingId,
            EmailType = log.EmailType,
            Status = log.Status,
            Subject = log.Subject,
            ToRecipientsJson = log.ToRecipientsJson,
            CcRecipientsJson = log.CcRecipientsJson,
            RequestedAt = log.RequestedAt,
            SentAt = log.SentAt,
            ErrorMessage = log.ErrorMessage,
            BodyHtml = _settings.StoreEmailBody ? log.BodyHtml : null,
            BodyText = _settings.StoreEmailBody ? log.BodyText : null
        };
    }
}
