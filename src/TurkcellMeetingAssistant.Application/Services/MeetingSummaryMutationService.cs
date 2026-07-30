using Microsoft.EntityFrameworkCore;
using TurkcellMeetingAssistant.Application.Common.Exceptions;
using TurkcellMeetingAssistant.Application.Interfaces;
using TurkcellMeetingAssistant.Domain.Entities;
using TurkcellMeetingAssistant.Domain.Enums;

namespace TurkcellMeetingAssistant.Application.Services;

public class MeetingSummaryMutationService : IMeetingSummaryMutationService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public MeetingSummaryMutationService(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<(Meeting Meeting, MeetingSummary Summary)> GetActiveSummaryForMutationAsync(
        Guid meetingId, 
        int? expectedVersion = null, 
        int? expectedManualRevisionNumber = null,
        bool requireOwnership = true,
        CancellationToken cancellationToken = default)
    {
        var meeting = await _context.Meetings
            .Include(m => m.Summaries.Where(s => !s.IsDeleted).OrderByDescending(s => s.Version).Take(1))
            .FirstOrDefaultAsync(m => m.Id == meetingId, cancellationToken);

        if (meeting == null)
            throw new NotFoundException(nameof(Meeting), meetingId);

        if (requireOwnership && _currentUserService.Role != UserRole.Admin.ToString() && meeting.OrganizerUserId != _currentUserService.UserId)
            throw new ForbiddenAccessException();

        if (meeting.Status == MeetingStatus.Archived)
            throw new BadRequestException("Arşivlenmiş toplantılar güncellenemez.");
            
        if (meeting.Status == MeetingStatus.EmailSent)
            throw new BadRequestException("Özeti e-posta ile gönderilmiş toplantılar güncellenemez.");
            
        if (meeting.Status == MeetingStatus.Analyzing)
            throw new BadRequestException("Toplantı analizi devam ederken güncelleme yapılamaz.");

        var summary = meeting.Summaries.FirstOrDefault();
        if (summary == null)
            throw new NotFoundException("Aktif toplantı özeti bulunamadı.");

        if (expectedVersion.HasValue && summary.Version != expectedVersion.Value)
            throw new ConcurrencyException("Özetin versiyonu değişmiş. Lütfen sayfayı yenileyin.");

        if (expectedManualRevisionNumber.HasValue && summary.ManualRevisionNumber != expectedManualRevisionNumber.Value)
            throw new ConcurrencyException("Bu kayıt başka bir kullanıcı tarafından güncellendi. Lütfen sayfayı yenileyip tekrar deneyin.");

        return (meeting, summary);
    }

    public void MutateSummary(Meeting meeting, MeetingSummary summary, string actionName, string? changedFields = null)
    {
        summary.ManualRevisionNumber++;
        summary.LastEditedAt = DateTime.UtcNow;
        summary.LastEditedByUserId = _currentUserService.UserId;

        if (summary.IsApproved)
        {
            summary.IsApproved = false;
            summary.ApprovedAt = null;
            summary.ApprovedByUserId = null;
            summary.ApprovedContentHash = null;
            summary.ApprovalRevokedAt = DateTime.UtcNow;
            summary.ApprovalRevokedReason = "İçerik değişikliği sebebiyle onay otomatik iptal edildi.";
            
            LogAudit("MeetingSummaryApprovalRevoked", nameof(MeetingSummary), summary.Id.ToString(), "Approval removed due to summary content changes.");
        }
        
        meeting.Status = MeetingStatus.WaitingForApproval;

        var auditDetails = $"Revision: {summary.ManualRevisionNumber}";
        if (!string.IsNullOrEmpty(changedFields))
            auditDetails += $", ChangedFields: {changedFields}";
            
        LogAudit(actionName, nameof(MeetingSummary), summary.Id.ToString(), auditDetails);
    }

    public void LogAudit(string action, string entityName, string entityId, string newValues)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = _currentUserService.UserId,
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            CreatedAt = DateTime.UtcNow,
            NewValues = newValues
        });
    }
}
