using Microsoft.EntityFrameworkCore;
using TurkcellMeetingAssistant.Application.Common.Exceptions;
using TurkcellMeetingAssistant.Application.Common.Models;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Analysis;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Summary;
using TurkcellMeetingAssistant.Application.Interfaces;
using TurkcellMeetingAssistant.Domain.Entities;
using TurkcellMeetingAssistant.Domain.Enums;

namespace TurkcellMeetingAssistant.Application.Services;

public class MeetingActionItemService : IMeetingActionItemService
{
    private readonly IApplicationDbContext _context;
    private readonly IMeetingSummaryMutationService _mutationService;
    private readonly ICurrentUserService _currentUserService;

    public MeetingActionItemService(
        IApplicationDbContext context,
        IMeetingSummaryMutationService mutationService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _mutationService = mutationService;
        _currentUserService = currentUserService;
    }

    public async Task<ActionItemDto> AddActionItemAsync(Guid meetingId, CreateActionItemRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var (meeting, summary) = await _mutationService.GetActiveSummaryForMutationAsync(meetingId, null, null, true, cancellationToken);
            
            var ownerName = request.OwnerName;
            var ownerEmail = request.OwnerEmail;
            
            if (request.AssignedParticipantId.HasValue)
            {
                var participant = await _context.MeetingParticipants.FirstOrDefaultAsync(p => p.Id == request.AssignedParticipantId.Value && p.MeetingId == meetingId, cancellationToken);
                if (participant == null) throw new BadRequestException("Belirtilen katılımcı bu toplantıya ait değil.");
                ownerName = participant.FullName;
                ownerEmail = participant.Email;
            }

            var action = new ActionItem
            {
                Id = Guid.NewGuid(),
                MeetingId = meetingId,
                MeetingSummaryId = summary.Id,
                Description = request.Description,
                OwnerName = ownerName,
                OwnerEmail = ownerEmail,
                AssignedParticipantId = request.AssignedParticipantId,
                DueDate = request.DueDate,
                Priority = request.Priority,
                Status = request.Status ?? ActionItemStatus.Open,
                ConfidenceScore = request.ConfidenceScore,
                Evidence = request.Evidence,
                SourceType = SourceType.Manual,
                CreatedAt = DateTime.UtcNow,
                LastEditedAt = DateTime.UtcNow,
                LastEditedByUserId = _currentUserService.UserId
            };
            
            _context.ActionItems.Add(action);
            _mutationService.MutateSummary(meeting, summary, "ActionItemAdded");
            
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            return MapActionItemDto(action, meeting.Title);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ActionItemDto> UpdateActionItemAsync(Guid meetingId, Guid actionItemId, UpdateActionItemRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var (meeting, summary) = await _mutationService.GetActiveSummaryForMutationAsync(meetingId, null, null, true, cancellationToken);
            var action = await _context.ActionItems.FirstOrDefaultAsync(a => a.Id == actionItemId && a.MeetingSummaryId == summary.Id, cancellationToken);
            
            if (action == null) throw new NotFoundException(nameof(ActionItem), actionItemId);
            
            if (request.ExpectedUpdatedAt.HasValue && action.UpdatedAt.HasValue)
            {
                // This truncates the milliseconds to correctly compare postgres timestamp
                if (Math.Abs((action.UpdatedAt.Value - request.ExpectedUpdatedAt.Value).TotalSeconds) > 1)
                    throw new ConcurrencyException("Aksiyon başka biri tarafından değiştirildi.");
            }

            if (action.Status == ActionItemStatus.Completed)
            {
                 // Gerekirse completed iş kuralı
            }

            var ownerName = request.OwnerName;
            var ownerEmail = request.OwnerEmail;
            
            if (request.AssignedParticipantId.HasValue)
            {
                var participant = await _context.MeetingParticipants.FirstOrDefaultAsync(p => p.Id == request.AssignedParticipantId.Value && p.MeetingId == meetingId, cancellationToken);
                if (participant == null) throw new BadRequestException("Belirtilen katılımcı bu toplantıya ait değil.");
                ownerName = participant.FullName;
                ownerEmail = participant.Email;
            }

            action.Description = request.Description;
            action.OwnerName = ownerName;
            action.OwnerEmail = ownerEmail;
            action.AssignedParticipantId = request.AssignedParticipantId;
            action.DueDate = request.DueDate;
            action.Priority = request.Priority;
            action.ConfidenceScore = request.ConfidenceScore;
            action.Evidence = request.Evidence;
            action.LastEditedAt = DateTime.UtcNow;
            action.LastEditedByUserId = _currentUserService.UserId;
            
            _mutationService.MutateSummary(meeting, summary, "ActionItemUpdated");
            
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            return MapActionItemDto(action, meeting.Title);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteActionItemAsync(Guid meetingId, Guid actionItemId, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var (meeting, summary) = await _mutationService.GetActiveSummaryForMutationAsync(meetingId, null, null, true, cancellationToken);
            var action = await _context.ActionItems.FirstOrDefaultAsync(a => a.Id == actionItemId && a.MeetingSummaryId == summary.Id, cancellationToken);
            if (action == null) throw new NotFoundException(nameof(ActionItem), actionItemId);

            action.IsDeleted = true;
            action.DeletedAt = DateTime.UtcNow;
            
            _mutationService.MutateSummary(meeting, summary, "ActionItemRemoved");
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ActionItemDto> UpdateActionItemStatusAsync(Guid meetingId, Guid actionItemId, UpdateActionItemStatusRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var (meeting, summary) = await _mutationService.GetActiveSummaryForMutationAsync(meetingId, null, null, true, cancellationToken);
            var action = await _context.ActionItems.FirstOrDefaultAsync(a => a.Id == actionItemId && a.MeetingSummaryId == summary.Id, cancellationToken);
            
            if (action == null) throw new NotFoundException(nameof(ActionItem), actionItemId);
            
            // Validating state transitions
            if (action.Status == ActionItemStatus.Completed && request.Status != ActionItemStatus.Open)
                throw new BadRequestException("Completed aksiyon yalnızca Open durumuna alınabilir.");

            if (request.Status == ActionItemStatus.Cancelled && string.IsNullOrWhiteSpace(request.CancellationReason))
                throw new BadRequestException("İptal nedeni girilmelidir.");

            if (request.Status == ActionItemStatus.Completed)
            {
                action.CompletedAt = DateTime.UtcNow;
                action.CompletedByUserId = _currentUserService.UserId;
            }
            else if (action.Status == ActionItemStatus.Completed)
            {
                action.CompletedAt = null;
                action.CompletedByUserId = null;
            }

            if (request.Status == ActionItemStatus.Cancelled)
            {
                action.CancelledAt = DateTime.UtcNow;
                action.CancellationReason = request.CancellationReason;
            }
            else if (action.Status == ActionItemStatus.Cancelled)
            {
                action.CancelledAt = null;
                action.CancellationReason = null;
            }

            action.Status = request.Status;
            
            // Note: Status change does not mutate summary revision or revoke approval
            _mutationService.LogAudit("ActionItemStatusChanged", nameof(ActionItem), action.Id.ToString(), $"NewStatus: {action.Status}");
            
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            return MapActionItemDto(action, meeting.Title);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
    
    public async Task<PaginatedList<ActionItemDto>> GetActionItemsAsync(ActionItemFilterDto filter, CancellationToken cancellationToken)
    {
        var query = _context.ActionItems.AsNoTracking().Where(a => !a.IsDeleted);

        // Security
        if (_currentUserService.Role != UserRole.Admin.ToString())
        {
            query = query.Where(a => a.Meeting!.OrganizerUserId == _currentUserService.UserId);
        }
        
        return await BuildActionItemListAsync(query, filter, cancellationToken);
    }

    public async Task<PaginatedList<ActionItemDto>> GetMyActionItemsAsync(ActionItemFilterDto filter, CancellationToken cancellationToken)
    {
        var query = _context.ActionItems.AsNoTracking().Where(a => !a.IsDeleted);

        var currentUser = await _context.Users.FindAsync(new object[] { _currentUserService.UserId! }, cancellationToken);
        if (currentUser == null) throw new UnauthorizedAccessException();
        
        var userEmail = currentUser.Email.ToLower();
        query = query.Where(a => a.OwnerEmail != null && a.OwnerEmail.ToLower() == userEmail);

        return await BuildActionItemListAsync(query, filter, cancellationToken);
    }
    
    private async Task<PaginatedList<ActionItemDto>> BuildActionItemListAsync(IQueryable<ActionItem> query, ActionItemFilterDto filter, CancellationToken cancellationToken)
    {
        if (filter.MeetingId.HasValue)
            query = query.Where(a => a.MeetingId == filter.MeetingId.Value);
            
        if (!string.IsNullOrWhiteSpace(filter.OwnerEmail))
            query = query.Where(a => a.OwnerEmail != null && a.OwnerEmail.ToLower().Contains(filter.OwnerEmail.ToLower()));
            
        if (filter.AssignedParticipantId.HasValue)
            query = query.Where(a => a.AssignedParticipantId == filter.AssignedParticipantId.Value);
            
        if (filter.Status.HasValue)
            query = query.Where(a => a.Status == filter.Status.Value);
            
        if (filter.Priority.HasValue)
            query = query.Where(a => a.Priority == filter.Priority.Value);
            
        if (filter.DueDateFrom.HasValue)
            query = query.Where(a => a.DueDate >= filter.DueDateFrom.Value);
            
        if (filter.DueDateTo.HasValue)
            query = query.Where(a => a.DueDate <= filter.DueDateTo.Value);
            
        if (filter.IsOverdue.HasValue)
        {
            var now = DateTime.UtcNow.Date;
            if (filter.IsOverdue.Value)
            {
                query = query.Where(a => a.Status != ActionItemStatus.Completed && a.Status != ActionItemStatus.Cancelled && a.DueDate.HasValue && a.DueDate.Value < now);
            }
        }
        
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(a => a.Description.ToLower().Contains(search) || (a.OwnerName != null && a.OwnerName.ToLower().Contains(search)));
        }

        query = filter.SortBy?.ToLower() switch
        {
            "duedate" => filter.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(a => a.DueDate) : query.OrderBy(a => a.DueDate),
            "priority" => filter.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(a => a.Priority) : query.OrderBy(a => a.Priority),
            "status" => filter.SortDirection?.ToLower() == "desc" ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
            _ => query.OrderByDescending(a => a.CreatedAt)
        };
        
        var count = await query.CountAsync(cancellationToken);
        
        // Use projection to avoid fetching entire Meeting or Summary objects
        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(a => new ActionItemDto
            {
                Id = a.Id,
                MeetingId = a.MeetingId,
                MeetingTitle = a.Meeting != null ? a.Meeting.Title : string.Empty,
                Description = a.Description,
                OwnerName = a.OwnerName,
                OwnerEmail = a.OwnerEmail,
                DueDate = a.DueDate,
                Priority = a.Priority,
                Status = a.Status,
                ConfidenceScore = a.ConfidenceScore,
                Evidence = a.Evidence,
                CreatedAt = a.CreatedAt,
                AssignedParticipantId = a.AssignedParticipantId,
                SourceType = a.SourceType,
                LastEditedAt = a.LastEditedAt,
                CompletedAt = a.CompletedAt,
                CancellationReason = a.CancellationReason,
                IsOverdue = a.Status != ActionItemStatus.Completed && a.Status != ActionItemStatus.Cancelled && a.DueDate.HasValue && a.DueDate.Value.Date < DateTime.UtcNow.Date
            })
            .ToListAsync(cancellationToken);
            
        return new PaginatedList<ActionItemDto>(items, count, filter.PageNumber, filter.PageSize);
    }
    
    public async Task<ActionItemDto> GetActionItemAsync(Guid actionItemId, CancellationToken cancellationToken)
    {
        var action = await _context.ActionItems
            .AsNoTracking()
            .Include(a => a.Meeting)
            .FirstOrDefaultAsync(a => a.Id == actionItemId && !a.IsDeleted, cancellationToken);
            
        if (action == null) throw new NotFoundException(nameof(ActionItem), actionItemId);
        
        if (_currentUserService.Role != UserRole.Admin.ToString() && action.Meeting!.OrganizerUserId != _currentUserService.UserId)
        {
            var currentUser = await _context.Users.FindAsync(new object[] { _currentUserService.UserId! }, cancellationToken);
            if (currentUser == null || (action.OwnerEmail != null && action.OwnerEmail.ToLower() != currentUser.Email.ToLower()))
                throw new ForbiddenAccessException();
        }
        
        return MapActionItemDto(action, action.Meeting?.Title);
    }
    
    private ActionItemDto MapActionItemDto(ActionItem action, string? meetingTitle)
    {
        return new ActionItemDto
        {
            Id = action.Id,
            MeetingId = action.MeetingId,
            MeetingTitle = meetingTitle,
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
