using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using TurkcellMeetingAssistant.Application.Common.Exceptions;
using TurkcellMeetingAssistant.Application.DTOs.Meetings;
using TurkcellMeetingAssistant.Application.Interfaces;
using TurkcellMeetingAssistant.Domain.Entities;
using TurkcellMeetingAssistant.Domain.Enums;

namespace TurkcellMeetingAssistant.Application.Services;

public class TranscriptService : ITranscriptService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public TranscriptService(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task UpdateTranscriptTextAsync(Guid meetingId, TranscriptTextUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var meeting = await GetMeetingWithAccessCheckAsync(meetingId, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.TranscriptText) || request.TranscriptText.Length < 20)
            throw new BadRequestException("Transcript metni en az 20 karakter olmalıdır.");

        if (request.TranscriptText.Length > 500000)
            throw new BadRequestException("Transcript metni çok uzun.");

        meeting.TranscriptText = request.TranscriptText;
        meeting.TranscriptFileName = null;
        meeting.TranscriptFileType = "text/plain";
        meeting.TranscriptUploadedAt = DateTime.UtcNow;
        meeting.Status = MeetingStatus.ReadyForAnalysis;
        meeting.UpdatedAt = DateTime.UtcNow;

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = _currentUserService.UserId,
            EntityName = nameof(Meeting),
            EntityId = meeting.Id.ToString(),
            Action = "TranscriptTextUpdated",
            CreatedAt = DateTime.UtcNow
            // Note: intentionally not saving TranscriptText into OldValues/NewValues
        };
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessTranscriptFileAsync(Guid meetingId, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var meeting = await GetMeetingWithAccessCheckAsync(meetingId, cancellationToken);

        if (fileStream.Length == 0 || fileStream.Length > 10 * 1024 * 1024)
            throw new BadRequestException("Dosya boyutu geçersiz (Maksimum 10 MB).");

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext != ".txt" && ext != ".vtt")
            throw new BadRequestException("Sadece .txt ve .vtt uzantılı dosyalar desteklenmektedir.");

        using var reader = new StreamReader(fileStream);
        var content = await reader.ReadToEndAsync();

        if (ext == ".vtt")
        {
            content = ParseVtt(content);
        }

        if (string.IsNullOrWhiteSpace(content) || content.Length < 20)
            throw new BadRequestException("Çıkarılan metin çok kısa veya geçersiz.");
            
        if (content.Length > 500000)
            throw new BadRequestException("Çıkarılan metin çok uzun.");

        meeting.TranscriptText = content;
        meeting.TranscriptFileName = Path.GetFileName(fileName); // Sanitize simply
        meeting.TranscriptFileType = contentType;
        meeting.TranscriptUploadedAt = DateTime.UtcNow;
        meeting.Status = MeetingStatus.ReadyForAnalysis;
        meeting.UpdatedAt = DateTime.UtcNow;

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = _currentUserService.UserId,
            EntityName = nameof(Meeting),
            EntityId = meeting.Id.ToString(),
            Action = "TranscriptFileUploaded",
            CreatedAt = DateTime.UtcNow
        };
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<TranscriptResponseDto> GetTranscriptAsync(Guid meetingId, CancellationToken cancellationToken = default)
    {
        var meeting = await GetMeetingWithAccessCheckAsync(meetingId, cancellationToken);

        if (string.IsNullOrEmpty(meeting.TranscriptText))
            throw new NotFoundException("Transcript", meetingId);

        return new TranscriptResponseDto
        {
            MeetingId = meeting.Id,
            TranscriptText = meeting.TranscriptText,
            TranscriptFileName = meeting.TranscriptFileName,
            TranscriptFileType = meeting.TranscriptFileType,
            TranscriptUploadedAt = meeting.TranscriptUploadedAt,
            CharacterCount = meeting.TranscriptText.Length,
            WordCount = meeting.TranscriptText.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length
        };
    }

    public async Task DeleteTranscriptAsync(Guid meetingId, CancellationToken cancellationToken = default)
    {
        var meeting = await GetMeetingWithAccessCheckAsync(meetingId, cancellationToken);

        if (meeting.Status == MeetingStatus.AnalysisCompleted || meeting.Status == MeetingStatus.Approved || meeting.Status == MeetingStatus.EmailSent)
            throw new BadRequestException("Analizi tamamlanmış veya onaylanmış toplantının metni silinemez.");

        meeting.TranscriptText = null;
        meeting.TranscriptFileName = null;
        meeting.TranscriptFileType = null;
        meeting.TranscriptUploadedAt = null;
        
        if (meeting.Status == MeetingStatus.ReadyForAnalysis || meeting.Status == MeetingStatus.Failed)
            meeting.Status = MeetingStatus.Draft;
            
        meeting.UpdatedAt = DateTime.UtcNow;

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = _currentUserService.UserId,
            EntityName = nameof(Meeting),
            EntityId = meeting.Id.ToString(),
            Action = "TranscriptDeleted",
            CreatedAt = DateTime.UtcNow
        };
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Meeting> GetMeetingWithAccessCheckAsync(Guid meetingId, CancellationToken cancellationToken)
    {
        var meeting = await _context.Meetings.FirstOrDefaultAsync(x => x.Id == meetingId, cancellationToken);
        if (meeting == null)
            throw new NotFoundException(nameof(Meeting), meetingId);

        var isUserAdmin = _currentUserService.Role == UserRole.Admin.ToString();
        if (!isUserAdmin && meeting.OrganizerUserId != _currentUserService.UserId)
            throw new NotFoundException(nameof(Meeting), meetingId);

        if (meeting.IsArchived)
            throw new BadRequestException("Arşivlenmiş toplantıda transcript işlemi yapılamaz.");

        return meeting;
    }

    private string ParseVtt(string vttContent)
    {
        var lines = vttContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var parsedText = new List<string>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("WEBVTT")) continue;
            if (Regex.IsMatch(line, @"\d{2}:\d{2}:\d{2}\.\d{3}\s-->\s\d{2}:\d{2}:\d{2}\.\d{3}")) continue;
            if (Regex.IsMatch(line, @"\d{2}:\d{2}\.\d{3}\s-->\s\d{2}:\d{2}\.\d{3}")) continue;

            // Remove speaker tags like <v Speaker Name>
            var cleanLine = Regex.Replace(line, @"<v [^>]+>", "");
            cleanLine = cleanLine.Replace("</v>", "");

            parsedText.Add(cleanLine.Trim());
        }

        return string.Join(" ", parsedText).Trim();
    }
}
