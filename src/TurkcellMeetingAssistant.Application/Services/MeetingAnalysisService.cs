using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TurkcellMeetingAssistant.Application.Common.Exceptions;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Analysis;
using TurkcellMeetingAssistant.Application.Interfaces;
using TurkcellMeetingAssistant.Domain.Entities;
using TurkcellMeetingAssistant.Domain.Enums;
using FluentValidation;

namespace TurkcellMeetingAssistant.Application.Services;

public class MeetingAnalysisService : IMeetingAnalysisService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAiMeetingAnalysisService _aiAnalysisService;
    private readonly IValidator<MeetingAnalysisResult> _validator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MeetingAnalysisService> _logger;

    public MeetingAnalysisService(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IAiMeetingAnalysisService aiAnalysisService,
        IValidator<MeetingAnalysisResult> validator,
        IConfiguration configuration,
        ILogger<MeetingAnalysisService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _aiAnalysisService = aiAnalysisService;
        _validator = validator;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<MeetingAnalysisStatusDto> AnalyzeMeetingAsync(Guid meetingId, CancellationToken cancellationToken)
    {
        var meeting = await GetMeetingAndCheckAccessAsync(meetingId, cancellationToken);

        if (meeting.IsArchived)
            throw new BadRequestException("Arşivlenmiş toplantı analiz edilemez.");

        if (meeting.Status == MeetingStatus.EmailSent)
            throw new BadRequestException("Toplantı özeti e-postası gönderildiği için yeniden analiz edilemez.");

        if (meeting.Status == MeetingStatus.Analyzing)
            throw new AnalysisAlreadyInProgressException("Toplantı şu anda analiz ediliyor.");

        if (string.IsNullOrWhiteSpace(meeting.TranscriptText))
            throw new TranscriptNotFoundException("Toplantıya ait konuşma metni (transcript) bulunamadı.");

        var minLength = _configuration.GetValue<int>("MeetingAnalysis:MinimumTranscriptLength", 50);
        if (meeting.TranscriptText.Length < minLength)
            throw new TranscriptTooShortException($"Konuşma metni analiz için çok kısa. Minimum {minLength} karakter olmalıdır.");

        // Change status to Analyzing
        meeting.Status = MeetingStatus.Analyzing;
        
        // This normally is just `DateTime.UtcNow` but let's record it maybe in Meeting entity if we add `AnalysisStartedAt` later. 
        // For now, we just proceed.
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            var result = await PerformAnalysisAsync(meeting, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Toplantı analizi sırasında hata oluştu. MeetingId: {MeetingId}", meetingId);
            
            meeting.Status = MeetingStatus.Failed;
            
            var log = new AiAnalysisLog
            {
                MeetingId = meetingId,
                Provider = _configuration["MeetingAnalysis:Provider"] ?? "Mock",
                PromptVersion = _configuration["MeetingAnalysis:PromptVersion"] ?? "v1",
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.AiAnalysisLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
            
            throw;
        }
    }

    public async Task<MeetingAnalysisStatusDto> ReanalyzeMeetingAsync(Guid meetingId, CancellationToken cancellationToken)
    {
        // Reanalyze has the same basic checks as analyze for now
        return await AnalyzeMeetingAsync(meetingId, cancellationToken);
    }

    public async Task<MeetingAnalysisStatusDto> GetAnalysisStatusAsync(Guid meetingId, CancellationToken cancellationToken)
    {
        var meeting = await GetMeetingAndCheckAccessAsync(meetingId, cancellationToken);
        
        var currentSummary = await _context.MeetingSummaries
            .Where(x => x.MeetingId == meetingId)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(cancellationToken);
            
        var lastLog = await _context.AiAnalysisLogs
            .Where(x => x.MeetingId == meetingId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return new MeetingAnalysisStatusDto
        {
            MeetingId = meeting.Id,
            Status = meeting.Status,
            HasTranscript = !string.IsNullOrWhiteSpace(meeting.TranscriptText),
            HasAnalysis = currentSummary != null,
            CurrentSummaryId = currentSummary?.Id,
            CurrentVersion = currentSummary?.Version ?? 0,
            LastErrorMessage = meeting.Status == MeetingStatus.Failed ? lastLog?.ErrorMessage : null,
            CanAnalyze = meeting.Status == MeetingStatus.ReadyForAnalysis || meeting.Status == MeetingStatus.Draft,
            CanReanalyze = meeting.Status == MeetingStatus.AnalysisCompleted || meeting.Status == MeetingStatus.WaitingForApproval || meeting.Status == MeetingStatus.Failed
        };
    }

    public async Task<MeetingSummaryDto> GetMeetingSummaryAsync(Guid meetingId, CancellationToken cancellationToken)
    {
        var meeting = await GetMeetingAndCheckAccessAsync(meetingId, cancellationToken);
        
        var summary = await _context.MeetingSummaries
            .Include(x => x.Topics)
            .Include(x => x.Decisions)
            .Include(x => x.ActionItems)
            .Include(x => x.OpenIssues)
            .Where(x => x.MeetingId == meetingId)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(cancellationToken);
            
        if (summary == null)
            throw new NotFoundException(nameof(MeetingSummary), meetingId);
            
        return new MeetingSummaryDto
        {
            MeetingId = summary.MeetingId,
            SummaryId = summary.Id,
            Version = summary.Version,
            MeetingPurpose = summary.MeetingPurpose,
            ExecutiveSummary = summary.ExecutiveSummary,
            IsApproved = summary.IsApproved,
            ApprovedAt = summary.ApprovedAt,
            Provider = summary.AiProvider,
            ModelName = summary.AiModelName,
            PromptVersion = summary.PromptVersion,
            CreatedAt = summary.CreatedAt,
            UpdatedAt = summary.UpdatedAt,
            Topics = summary.Topics.OrderBy(x => x.SortOrder).Select(t => new MeetingTopicDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                SortOrder = t.SortOrder
            }).ToList(),
            Decisions = summary.Decisions.OrderBy(x => x.SortOrder).Select(d => new MeetingDecisionDto
            {
                Id = d.Id,
                Description = d.Description,
                RelatedTopic = d.RelatedTopic,
                ConfidenceScore = d.ConfidenceScore,
                Evidence = d.Evidence,
                SortOrder = d.SortOrder
            }).ToList(),
            ActionItems = summary.ActionItems.Select(a => new ActionItemDto
            {
                Id = a.Id,
                Description = a.Description,
                OwnerName = a.OwnerName,
                OwnerEmail = a.OwnerEmail,
                DueDate = a.DueDate,
                Priority = a.Priority,
                Status = a.Status,
                ConfidenceScore = a.ConfidenceScore,
                Evidence = a.Evidence,
                CreatedAt = a.CreatedAt
            }).ToList(),
            OpenIssues = summary.OpenIssues.OrderBy(x => x.SortOrder).Select(o => new OpenIssueDto
            {
                Id = o.Id,
                Description = o.Description,
                OwnerName = o.OwnerName,
                SortOrder = o.SortOrder
            }).ToList()
        };
    }

    private async Task<Meeting> GetMeetingAndCheckAccessAsync(Guid meetingId, CancellationToken cancellationToken)
    {
        var meeting = await _context.Meetings
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x => x.Id == meetingId, cancellationToken);

        if (meeting == null)
            throw new NotFoundException(nameof(Meeting), meetingId);

        var isUserAdmin = _currentUserService.Role == UserRole.Admin.ToString();
        if (!isUserAdmin && meeting.OrganizerUserId != _currentUserService.UserId)
            throw new NotFoundException(nameof(Meeting), meetingId);

        return meeting;
    }

    private async Task<MeetingAnalysisStatusDto> PerformAnalysisAsync(Meeting meeting, CancellationToken cancellationToken)
    {
        var promptVersion = _configuration["MeetingAnalysis:PromptVersion"] ?? "v1";
        var provider = _configuration["MeetingAnalysis:Provider"] ?? "Mock";
        
        var input = new MeetingAnalysisInput
        {
            MeetingId = meeting.Id,
            MeetingTitle = meeting.Title,
            MeetingDate = meeting.MeetingDate,
            TranscriptText = meeting.TranscriptText!,
            Language = "tr",
            PromptVersion = promptVersion,
            Participants = meeting.Participants.Select(p => new AnalysisParticipantDto
            {
                FullName = p.FullName,
                Email = p.Email,
                Title = p.Title
            }).ToList()
        };

        var watch = System.Diagnostics.Stopwatch.StartNew();
        
        var aiResult = await _aiAnalysisService.AnalyzeAsync(input, cancellationToken);
        
        watch.Stop();

        // Validate Result
        var validationResult = await _validator.ValidateAsync(aiResult, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AiResultValidationException("AI servisinden gelen sonuç doğrulama kurallarına uymadı.", validationResult.Errors);
        }

        // Determine next version
        var currentVersion = await _context.MeetingSummaries
            .Where(x => x.MeetingId == meeting.Id)
            .MaxAsync(x => (int?)x.Version, cancellationToken) ?? 0;
            
        var nextVersion = currentVersion + 1;

        // Map to Entities inside transaction
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var summary = new MeetingSummary
            {
                Id = Guid.NewGuid(),
                MeetingId = meeting.Id,
                MeetingPurpose = aiResult.MeetingPurpose,
                ExecutiveSummary = aiResult.ExecutiveSummary,
                Version = nextVersion,
                IsApproved = false,
                AiProvider = provider,
                PromptVersion = promptVersion,
                CreatedAt = DateTime.UtcNow
            };

            int topicSort = 1;
            foreach (var t in aiResult.Topics)
            {
                summary.Topics.Add(new MeetingTopic
                {
                    Id = Guid.NewGuid(),
                    Title = t.Title,
                    Description = t.Description,
                    SortOrder = topicSort++,
                    CreatedAt = DateTime.UtcNow
                });
            }

            int decisionSort = 1;
            foreach (var d in aiResult.Decisions)
            {
                summary.Decisions.Add(new MeetingDecision
                {
                    Id = Guid.NewGuid(),
                    Description = d.Description,
                    RelatedTopic = d.RelatedTopic,
                    ConfidenceScore = d.ConfidenceScore,
                    Evidence = d.Evidence,
                    SortOrder = decisionSort++,
                    CreatedAt = DateTime.UtcNow
                });
            }

            foreach (var a in aiResult.ActionItems)
            {
                summary.ActionItems.Add(new ActionItem
                {
                    Id = Guid.NewGuid(),
                    MeetingId = meeting.Id,
                    Description = a.Description,
                    OwnerName = a.OwnerName,
                    OwnerEmail = a.OwnerEmail,
                    DueDate = a.DueDate,
                    Priority = Enum.TryParse<ActionPriority>(a.Priority, out var parsedPriority) ? parsedPriority : ActionPriority.Medium,
                    Status = ActionItemStatus.Open,
                    ConfidenceScore = a.ConfidenceScore,
                    Evidence = a.Evidence,
                    CreatedAt = DateTime.UtcNow
                });
            }

            int openIssueSort = 1;
            foreach (var o in aiResult.OpenIssues)
            {
                summary.OpenIssues.Add(new OpenIssue
                {
                    Id = Guid.NewGuid(),
                    Description = o.Description,
                    OwnerName = o.OwnerName,
                    SortOrder = openIssueSort++,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _context.MeetingSummaries.Add(summary);
            
            // Log
            var storeRawResponse = _configuration.GetValue<bool>("MeetingAnalysis:StoreRawResponse", false);
            
            var aiLog = new AiAnalysisLog
            {
                Id = Guid.NewGuid(),
                MeetingId = meeting.Id,
                Provider = provider,
                PromptVersion = promptVersion,
                InputCharacterCount = input.TranscriptText.Length,
                DurationMilliseconds = watch.ElapsedMilliseconds,
                IsSuccessful = true,
                CreatedAt = DateTime.UtcNow,
                RawResponse = storeRawResponse ? "Mock raw response is empty here" : null
            };
            _context.AiAnalysisLogs.Add(aiLog);

            // Audit
            _context.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityName = nameof(Meeting),
                EntityId = meeting.Id.ToString(),
                Action = currentVersion == 0 ? "MeetingAnalysisStarted" : "MeetingReanalyzed",
                UserId = _currentUserService.UserId,
                CreatedAt = DateTime.UtcNow,
                NewValues = $"SummaryVersion: {nextVersion}, Provider: {provider}, Topics: {aiResult.Topics.Count}, Actions: {aiResult.ActionItems.Count}"
            });

            meeting.Status = MeetingStatus.WaitingForApproval;
            
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new MeetingAnalysisStatusDto
            {
                MeetingId = meeting.Id,
                Status = meeting.Status,
                HasTranscript = true,
                HasAnalysis = true,
                CurrentSummaryId = summary.Id,
                CurrentVersion = summary.Version,
                CanAnalyze = false,
                CanReanalyze = true
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
