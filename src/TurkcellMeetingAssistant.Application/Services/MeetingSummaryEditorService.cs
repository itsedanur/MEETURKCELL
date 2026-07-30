using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using TurkcellMeetingAssistant.Application.Common.Exceptions;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Analysis;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Summary;
using TurkcellMeetingAssistant.Application.Interfaces;
using TurkcellMeetingAssistant.Domain.Entities;
using TurkcellMeetingAssistant.Domain.Enums;

namespace TurkcellMeetingAssistant.Application.Services;

public class MeetingSummaryEditorService : IMeetingSummaryEditorService
{
    private readonly IApplicationDbContext _context;
    private readonly IMeetingSummaryMutationService _mutationService;
    private readonly ICurrentUserService _currentUserService;

    public MeetingSummaryEditorService(
        IApplicationDbContext context,
        IMeetingSummaryMutationService mutationService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _mutationService = mutationService;
        _currentUserService = currentUserService;
    }

    public async Task<MeetingSummaryDto> UpdateSummaryAsync(Guid meetingId, UpdateMeetingSummaryRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var (meeting, summary) = await _mutationService.GetActiveSummaryForMutationAsync(
                meetingId, request.ExpectedVersion, request.ExpectedManualRevisionNumber, true, cancellationToken);

            summary.MeetingPurpose = request.MeetingPurpose;
            summary.ExecutiveSummary = request.ExecutiveSummary;

            _mutationService.MutateSummary(meeting, summary, "MeetingSummaryUpdated", "MeetingPurpose, ExecutiveSummary");
            
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await GetSummaryVersionAsync(meetingId, summary.Version, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    // Implementing Topic methods
    public async Task<MeetingTopicDto> AddTopicAsync(Guid meetingId, CreateMeetingTopicRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var (meeting, summary) = await _mutationService.GetActiveSummaryForMutationAsync(meetingId, null, null, true, cancellationToken);
            
            var topic = new MeetingTopic
            {
                Id = Guid.NewGuid(),
                MeetingSummaryId = summary.Id,
                Title = request.Title,
                Description = request.Description,
                SortOrder = request.SortOrder,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.MeetingTopics.Add(topic);
            _mutationService.MutateSummary(meeting, summary, "MeetingTopicAdded");
            
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            return new MeetingTopicDto { Id = topic.Id, Title = topic.Title, Description = topic.Description, SortOrder = topic.SortOrder, CanEdit = true };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<MeetingTopicDto> UpdateTopicAsync(Guid meetingId, Guid topicId, UpdateMeetingTopicRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var (meeting, summary) = await _mutationService.GetActiveSummaryForMutationAsync(meetingId, null, null, true, cancellationToken);
            var topic = await _context.MeetingTopics.FirstOrDefaultAsync(t => t.Id == topicId && t.MeetingSummaryId == summary.Id, cancellationToken);
            
            if (topic == null) throw new NotFoundException(nameof(MeetingTopic), topicId);

            topic.Title = request.Title;
            topic.Description = request.Description;
            topic.SortOrder = request.SortOrder;
            
            _mutationService.MutateSummary(meeting, summary, "MeetingTopicUpdated");
            
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            return new MeetingTopicDto { Id = topic.Id, Title = topic.Title, Description = topic.Description, SortOrder = topic.SortOrder, CanEdit = true };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteTopicAsync(Guid meetingId, Guid topicId, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var (meeting, summary) = await _mutationService.GetActiveSummaryForMutationAsync(meetingId, null, null, true, cancellationToken);
            var topic = await _context.MeetingTopics.FirstOrDefaultAsync(t => t.Id == topicId && t.MeetingSummaryId == summary.Id, cancellationToken);
            if (topic == null) throw new NotFoundException(nameof(MeetingTopic), topicId);

            topic.IsDeleted = true;
            topic.DeletedAt = DateTime.UtcNow;
            
            _mutationService.MutateSummary(meeting, summary, "MeetingTopicRemoved");
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ReorderTopicsAsync(Guid meetingId, ReorderRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var (meeting, summary) = await _mutationService.GetActiveSummaryForMutationAsync(meetingId, null, null, true, cancellationToken);
            var topics = await _context.MeetingTopics.Where(t => t.MeetingSummaryId == summary.Id).ToListAsync(cancellationToken);
            
            if (topics.Count != request.ItemIds.Count || !topics.All(t => request.ItemIds.Contains(t.Id)))
                throw new BadRequestException("Reorder listesi geçersiz. Tüm id'ler eksiksiz gönderilmelidir.");

            if (request.ItemIds.Distinct().Count() != request.ItemIds.Count)
                throw new BadRequestException("Duplicate ID'ler bulunmakta.");

            for (int i = 0; i < request.ItemIds.Count; i++)
            {
                var topic = topics.First(t => t.Id == request.ItemIds[i]);
                topic.SortOrder = i;
            }

            _mutationService.MutateSummary(meeting, summary, "MeetingTopicsReordered");
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    // Implementing Decision methods
    public async Task<MeetingDecisionDto> AddDecisionAsync(Guid meetingId, CreateMeetingDecisionRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var (meeting, summary) = await _mutationService.GetActiveSummaryForMutationAsync(meetingId, null, null, true, cancellationToken);
            
            var decision = new MeetingDecision
            {
                Id = Guid.NewGuid(),
                MeetingSummaryId = summary.Id,
                Description = request.Description,
                RelatedTopic = request.RelatedTopic,
                ConfidenceScore = request.ConfidenceScore,
                Evidence = request.Evidence,
                SortOrder = request.SortOrder,
                SourceType = SourceType.Manual,
                CreatedAt = DateTime.UtcNow,
                LastEditedAt = DateTime.UtcNow,
                LastEditedByUserId = _currentUserService.UserId
            };
            
            _context.MeetingDecisions.Add(decision);
            _mutationService.MutateSummary(meeting, summary, "MeetingDecisionAdded");
            
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            return MapDecisionDto(decision, true);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<MeetingDecisionDto> UpdateDecisionAsync(Guid meetingId, Guid decisionId, UpdateMeetingDecisionRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var (meeting, summary) = await _mutationService.GetActiveSummaryForMutationAsync(meetingId, null, null, true, cancellationToken);
            var decision = await _context.MeetingDecisions.FirstOrDefaultAsync(d => d.Id == decisionId && d.MeetingSummaryId == summary.Id, cancellationToken);
            
            if (decision == null) throw new NotFoundException(nameof(MeetingDecision), decisionId);

            decision.Description = request.Description;
            decision.RelatedTopic = request.RelatedTopic;
            decision.ConfidenceScore = request.ConfidenceScore;
            decision.Evidence = request.Evidence;
            decision.SortOrder = request.SortOrder;
            decision.LastEditedAt = DateTime.UtcNow;
            decision.LastEditedByUserId = _currentUserService.UserId;
            
            _mutationService.MutateSummary(meeting, summary, "MeetingDecisionUpdated");
            
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            return MapDecisionDto(decision, true);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteDecisionAsync(Guid meetingId, Guid decisionId, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var (meeting, summary) = await _mutationService.GetActiveSummaryForMutationAsync(meetingId, null, null, true, cancellationToken);
            var decision = await _context.MeetingDecisions.FirstOrDefaultAsync(d => d.Id == decisionId && d.MeetingSummaryId == summary.Id, cancellationToken);
            if (decision == null) throw new NotFoundException(nameof(MeetingDecision), decisionId);

            decision.IsDeleted = true;
            decision.DeletedAt = DateTime.UtcNow;
            
            _mutationService.MutateSummary(meeting, summary, "MeetingDecisionRemoved");
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ReorderDecisionsAsync(Guid meetingId, ReorderRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var (meeting, summary) = await _mutationService.GetActiveSummaryForMutationAsync(meetingId, null, null, true, cancellationToken);
            var decisions = await _context.MeetingDecisions.Where(d => d.MeetingSummaryId == summary.Id).ToListAsync(cancellationToken);
            
            if (decisions.Count != request.ItemIds.Count || !decisions.All(d => request.ItemIds.Contains(d.Id)))
                throw new BadRequestException("Reorder listesi geçersiz.");
            if (request.ItemIds.Distinct().Count() != request.ItemIds.Count)
                throw new BadRequestException("Duplicate ID'ler bulunmakta.");

            for (int i = 0; i < request.ItemIds.Count; i++)
            {
                decisions.First(d => d.Id == request.ItemIds[i]).SortOrder = i;
            }

            _mutationService.MutateSummary(meeting, summary, "MeetingDecisionsReordered");
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    // Implementing OpenIssue methods
    public async Task<OpenIssueDto> AddOpenIssueAsync(Guid meetingId, CreateOpenIssueRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var (meeting, summary) = await _mutationService.GetActiveSummaryForMutationAsync(meetingId, null, null, true, cancellationToken);
            
            var issue = new OpenIssue
            {
                Id = Guid.NewGuid(),
                MeetingSummaryId = summary.Id,
                Description = request.Description,
                OwnerName = request.OwnerName,
                SortOrder = request.SortOrder,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.OpenIssues.Add(issue);
            _mutationService.MutateSummary(meeting, summary, "OpenIssueAdded");
            
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            return new OpenIssueDto { Id = issue.Id, Description = issue.Description, OwnerName = issue.OwnerName, SortOrder = issue.SortOrder, CanEdit = true };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<OpenIssueDto> UpdateOpenIssueAsync(Guid meetingId, Guid openIssueId, UpdateOpenIssueRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var (meeting, summary) = await _mutationService.GetActiveSummaryForMutationAsync(meetingId, null, null, true, cancellationToken);
            var issue = await _context.OpenIssues.FirstOrDefaultAsync(o => o.Id == openIssueId && o.MeetingSummaryId == summary.Id, cancellationToken);
            
            if (issue == null) throw new NotFoundException(nameof(OpenIssue), openIssueId);

            issue.Description = request.Description;
            issue.OwnerName = request.OwnerName;
            issue.SortOrder = request.SortOrder;
            
            _mutationService.MutateSummary(meeting, summary, "OpenIssueUpdated");
            
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            return new OpenIssueDto { Id = issue.Id, Description = issue.Description, OwnerName = issue.OwnerName, SortOrder = issue.SortOrder, CanEdit = true };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteOpenIssueAsync(Guid meetingId, Guid openIssueId, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var (meeting, summary) = await _mutationService.GetActiveSummaryForMutationAsync(meetingId, null, null, true, cancellationToken);
            var issue = await _context.OpenIssues.FirstOrDefaultAsync(o => o.Id == openIssueId && o.MeetingSummaryId == summary.Id, cancellationToken);
            if (issue == null) throw new NotFoundException(nameof(OpenIssue), openIssueId);

            issue.IsDeleted = true;
            issue.DeletedAt = DateTime.UtcNow;
            
            _mutationService.MutateSummary(meeting, summary, "OpenIssueRemoved");
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ReorderOpenIssuesAsync(Guid meetingId, ReorderRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var (meeting, summary) = await _mutationService.GetActiveSummaryForMutationAsync(meetingId, null, null, true, cancellationToken);
            var issues = await _context.OpenIssues.Where(o => o.MeetingSummaryId == summary.Id).ToListAsync(cancellationToken);
            
            if (issues.Count != request.ItemIds.Count || !issues.All(o => request.ItemIds.Contains(o.Id)))
                throw new BadRequestException("Reorder listesi geçersiz.");
            if (request.ItemIds.Distinct().Count() != request.ItemIds.Count)
                throw new BadRequestException("Duplicate ID'ler bulunmakta.");

            for (int i = 0; i < request.ItemIds.Count; i++)
            {
                issues.First(o => o.Id == request.ItemIds[i]).SortOrder = i;
            }

            _mutationService.MutateSummary(meeting, summary, "OpenIssuesReordered");
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    // Implementing Approval methods
    public async Task<ApproveMeetingSummaryResponse> ApproveSummaryAsync(Guid meetingId, ApproveMeetingSummaryRequest request, CancellationToken cancellationToken)
    {
        if (!request.Confirmation) throw new BadRequestException("Onay kutusu işaretlenmelidir.");

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var (meeting, summary) = await _mutationService.GetActiveSummaryForMutationAsync(meetingId, request.ExpectedVersion, request.ExpectedManualRevisionNumber, true, cancellationToken);

            if (string.IsNullOrWhiteSpace(summary.ExecutiveSummary))
                throw new BadRequestException("Yönetici özeti boş olamaz.");

            var actions = await _context.ActionItems.Where(a => a.MeetingSummaryId == summary.Id).ToListAsync(cancellationToken);
            if (actions.Any(a => string.IsNullOrWhiteSpace(a.Description)))
                throw new BadRequestException("Açıklaması boş olan aksiyonlar var.");
            if (actions.Any(a => a.DueDate.HasValue && a.DueDate.Value < DateTime.UtcNow.Date))
                throw new BadRequestException("Geçmiş tarihli aksiyon teslim tarihi var.");

            var decisions = await _context.MeetingDecisions.Where(d => d.MeetingSummaryId == summary.Id).ToListAsync(cancellationToken);
            if (decisions.Any(d => string.IsNullOrWhiteSpace(d.Description)))
                throw new BadRequestException("Açıklaması boş olan kararlar var.");

            if (!summary.IsApproved)
            {
                summary.IsApproved = true;
                summary.ApprovedAt = DateTime.UtcNow;
                summary.ApprovedByUserId = _currentUserService.UserId;
                
                using (var sha256 = SHA256.Create())
                {
                    var contentBytes = Encoding.UTF8.GetBytes(summary.ExecutiveSummary + "_" + summary.ManualRevisionNumber);
                    summary.ApprovedContentHash = Convert.ToBase64String(sha256.ComputeHash(contentBytes));
                }

                meeting.Status = MeetingStatus.Approved;
                
                _mutationService.LogAudit("MeetingSummaryApproved", nameof(MeetingSummary), summary.Id.ToString(), $"Version: {summary.Version}");
                await _context.SaveChangesAsync(cancellationToken);
            }
            
            await transaction.CommitAsync(cancellationToken);

            var user = await _context.Users.FindAsync(new object[] { summary.ApprovedByUserId! }, cancellationToken);

            var lowConfidenceItemsCount = actions.Count(a => a.ConfidenceScore < 0.70m) + decisions.Count(d => d.ConfidenceScore < 0.70m);

            return new ApproveMeetingSummaryResponse
            {
                MeetingId = meetingId,
                SummaryId = summary.Id,
                Version = summary.Version,
                ManualRevisionNumber = summary.ManualRevisionNumber,
                IsApproved = summary.IsApproved,
                ApprovedAt = summary.ApprovedAt,
                ApprovedBy = user != null ? $"{user.FirstName} {user.LastName}" : null,
                LowConfidenceItemCount = lowConfidenceItemsCount
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task RevokeApprovalAsync(Guid meetingId, RevokeApprovalRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var (meeting, summary) = await _mutationService.GetActiveSummaryForMutationAsync(meetingId, null, null, true, cancellationToken);

            if (!summary.IsApproved)
                throw new BadRequestException("Özet zaten onaylanmamış.");

            summary.IsApproved = false;
            summary.ApprovedAt = null;
            summary.ApprovedByUserId = null;
            summary.ApprovedContentHash = null;
            summary.ApprovalRevokedAt = DateTime.UtcNow;
            summary.ApprovalRevokedReason = request.Reason;
            
            meeting.Status = MeetingStatus.WaitingForApproval;
            
            _mutationService.LogAudit("MeetingSummaryApprovalRevoked", nameof(MeetingSummary), summary.Id.ToString(), $"Reason: {request.Reason}");
            
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<List<MeetingSummaryVersionDto>> GetSummaryVersionsAsync(Guid meetingId, CancellationToken cancellationToken)
    {
        var meeting = await _context.Meetings
            .Include(m => m.Summaries.OrderByDescending(s => s.Version))
                .ThenInclude(s => s.Topics)
            .Include(m => m.Summaries)
                .ThenInclude(s => s.Decisions)
            .Include(m => m.Summaries)
                .ThenInclude(s => s.ActionItems)
            .Include(m => m.Summaries)
                .ThenInclude(s => s.OpenIssues)
            .FirstOrDefaultAsync(m => m.Id == meetingId, cancellationToken);
            
        if (meeting == null) throw new NotFoundException(nameof(Meeting), meetingId);
        
        if (_currentUserService.Role != UserRole.Admin.ToString() && meeting.OrganizerUserId != _currentUserService.UserId)
            throw new ForbiddenAccessException();
            
        return meeting.Summaries.Select(s => new MeetingSummaryVersionDto
        {
            SummaryId = s.Id,
            Version = s.Version,
            ManualRevisionNumber = s.ManualRevisionNumber,
            IsApproved = s.IsApproved,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt,
            ApprovedAt = s.ApprovedAt,
            Provider = s.AiProvider,
            ModelName = s.AiModelName,
            PromptVersion = s.PromptVersion,
            TopicCount = s.Topics.Count(x => !x.IsDeleted),
            DecisionCount = s.Decisions.Count(x => !x.IsDeleted),
            ActionItemCount = s.ActionItems.Count(x => !x.IsDeleted),
            OpenIssueCount = s.OpenIssues.Count(x => !x.IsDeleted)
        }).ToList();
    }

    public async Task<MeetingSummaryDto> GetSummaryVersionAsync(Guid meetingId, int version, CancellationToken cancellationToken)
    {
        var meeting = await _context.Meetings
            .Include(m => m.Summaries.Where(s => s.Version == version))
                .ThenInclude(s => s.Topics.Where(t => !t.IsDeleted).OrderBy(t => t.SortOrder))
            .Include(m => m.Summaries.Where(s => s.Version == version))
                .ThenInclude(s => s.Decisions.Where(d => !d.IsDeleted).OrderBy(d => d.SortOrder))
            .Include(m => m.Summaries.Where(s => s.Version == version))
                .ThenInclude(s => s.ActionItems.Where(a => !a.IsDeleted))
            .Include(m => m.Summaries.Where(s => s.Version == version))
                .ThenInclude(s => s.OpenIssues.Where(o => !o.IsDeleted).OrderBy(o => o.SortOrder))
            .FirstOrDefaultAsync(m => m.Id == meetingId, cancellationToken);
            
        if (meeting == null) throw new NotFoundException(nameof(Meeting), meetingId);
        
        if (_currentUserService.Role != UserRole.Admin.ToString() && meeting.OrganizerUserId != _currentUserService.UserId)
            throw new ForbiddenAccessException();
            
        var summary = meeting.Summaries.FirstOrDefault();
        if (summary == null) throw new NotFoundException("Özet versiyonu bulunamadı.");
        
        // This is a minimal mapping. Usually Automapper is used.
        bool isLatest = !meeting.Summaries.Any(s => s.Version > version);
        bool canEdit = isLatest && (meeting.Status == MeetingStatus.AnalysisCompleted || meeting.Status == MeetingStatus.WaitingForApproval || meeting.Status == MeetingStatus.Approved);
        
        return new MeetingSummaryDto
        {
            MeetingId = meeting.Id,
            SummaryId = summary.Id,
            Version = summary.Version,
            MeetingPurpose = summary.MeetingPurpose,
            ExecutiveSummary = summary.ExecutiveSummary,
            IsApproved = summary.IsApproved,
            ApprovedAt = summary.ApprovedAt,
            Provider = summary.AiProvider,
            ModelName = summary.AiModelName,
            PromptVersion = summary.PromptVersion,
            ManualRevisionNumber = summary.ManualRevisionNumber,
            LastEditedAt = summary.LastEditedAt,
            CanEdit = canEdit,
            CanApprove = canEdit,
            Topics = summary.Topics.Select(t => new MeetingTopicDto { Id = t.Id, Title = t.Title, Description = t.Description, SortOrder = t.SortOrder, CanEdit = canEdit }).ToList(),
            Decisions = summary.Decisions.Select(d => MapDecisionDto(d, canEdit)).ToList(),
            ActionItems = summary.ActionItems.Select(a => MapActionItemDto(a)).ToList(),
            OpenIssues = summary.OpenIssues.Select(o => new OpenIssueDto { Id = o.Id, Description = o.Description, OwnerName = o.OwnerName, SortOrder = o.SortOrder, CanEdit = canEdit }).ToList()
        };
    }
    
    private MeetingDecisionDto MapDecisionDto(MeetingDecision decision, bool canEdit)
    {
        return new MeetingDecisionDto
        {
            Id = decision.Id,
            Description = decision.Description,
            RelatedTopic = decision.RelatedTopic,
            ConfidenceScore = decision.ConfidenceScore,
            Evidence = decision.Evidence,
            SortOrder = decision.SortOrder,
            SourceType = decision.SourceType,
            LastEditedAt = decision.LastEditedAt
        };
    }
    
    private ActionItemDto MapActionItemDto(ActionItem action)
    {
        return new ActionItemDto
        {
            Id = action.Id,
            Description = action.Description,
            OwnerName = action.OwnerName,
            OwnerEmail = action.OwnerEmail,
            DueDate = action.DueDate,
            Priority = action.Priority,
            Status = action.Status,
            ConfidenceScore = action.ConfidenceScore,
            Evidence = action.Evidence,
            CreatedAt = action.CreatedAt,
            AssignedParticipantId = action.AssignedParticipantId,
            SourceType = action.SourceType,
            IsOverdue = action.Status != ActionItemStatus.Completed && action.Status != ActionItemStatus.Cancelled && action.DueDate.HasValue && action.DueDate.Value.Date < DateTime.UtcNow.Date,
            LastEditedAt = action.LastEditedAt,
            CompletedAt = action.CompletedAt,
            CancellationReason = action.CancellationReason
        };
    }
}
