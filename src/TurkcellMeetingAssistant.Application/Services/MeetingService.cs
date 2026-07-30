using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TurkcellMeetingAssistant.Application.Common.Exceptions;
using TurkcellMeetingAssistant.Application.Common.Models;
using TurkcellMeetingAssistant.Application.DTOs.Meetings;
using TurkcellMeetingAssistant.Application.Interfaces;
using TurkcellMeetingAssistant.Domain.Entities;
using TurkcellMeetingAssistant.Domain.Enums;

namespace TurkcellMeetingAssistant.Application.Services;

public class MeetingService : IMeetingService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public MeetingService(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> CreateMeetingAsync(CreateMeetingRequest request, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId ?? Guid.Empty;
        if (currentUserId == Guid.Empty)
            throw new UnauthorizedAccessException("Yetkisiz erişim.");

        var meeting = new Meeting
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            MeetingDate = request.MeetingDate.ToUniversalTime(),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            OrganizerUserId = currentUserId,
            Status = MeetingStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        _context.Meetings.Add(meeting);

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = currentUserId,
            EntityName = nameof(Meeting),
            EntityId = meeting.Id.ToString(),
            Action = "MeetingCreated",
            CreatedAt = DateTime.UtcNow,
            NewValues = JsonSerializer.Serialize(new { meeting.Title, meeting.Status })
        };
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);

        return meeting.Id;
    }

    public async Task<PagedResult<MeetingListItemDto>> GetMeetingsAsync(
        int pageNumber,
        int pageSize,
        string? search,
        MeetingStatus? status,
        DateTime? meetingDateFrom,
        DateTime? meetingDateTo,
        bool isArchived = false,
        string sortBy = "MeetingDate",
        string sortDirection = "desc",
        CancellationToken cancellationToken = default)
    {
        var query = _context.Meetings.AsNoTracking().AsQueryable();

        var isUserAdmin = _currentUserService.Role == UserRole.Admin.ToString();
        if (!isUserAdmin)
        {
            var currentUserId = _currentUserService.UserId;
            query = query.Where(x => x.OrganizerUserId == currentUserId);
        }

        query = query.Where(x => x.IsArchived == isArchived);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(x => x.Title.ToLower().Contains(s) || (x.Description != null && x.Description.ToLower().Contains(s)));
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (meetingDateFrom.HasValue)
        {
            query = query.Where(x => x.MeetingDate >= meetingDateFrom.Value.ToUniversalTime());
        }

        if (meetingDateTo.HasValue)
        {
            query = query.Where(x => x.MeetingDate <= meetingDateTo.Value.ToUniversalTime());
        }

        query = sortDirection.ToLower() == "asc"
            ? (sortBy.ToLower() == "title" ? query.OrderBy(x => x.Title) : query.OrderBy(x => x.MeetingDate))
            : (sortBy.ToLower() == "title" ? query.OrderByDescending(x => x.Title) : query.OrderByDescending(x => x.MeetingDate));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new MeetingListItemDto
            {
                Id = x.Id,
                Title = x.Title,
                MeetingDate = x.MeetingDate,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                Status = x.Status,
                StatusDisplayName = x.Status.ToString(),
                ParticipantCount = x.Participants.Count,
                IsArchived = x.IsArchived,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<MeetingListItemDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<MeetingDetailDto> GetMeetingByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var meeting = await _context.Meetings
            .Include(x => x.OrganizerUser)
            .Include(x => x.Participants)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (meeting == null)
            throw new NotFoundException(nameof(Meeting), id);

        var isUserAdmin = _currentUserService.Role == UserRole.Admin.ToString();
        if (!isUserAdmin && meeting.OrganizerUserId != _currentUserService.UserId)
            throw new NotFoundException(nameof(Meeting), id); // Hide existence

        return new MeetingDetailDto
        {
            Id = meeting.Id,
            Title = meeting.Title,
            Description = meeting.Description,
            MeetingDate = meeting.MeetingDate,
            StartTime = meeting.StartTime,
            EndTime = meeting.EndTime,
            Organizer = meeting.OrganizerUser != null ? new UserSummaryDto
            {
                Id = meeting.OrganizerUser.Id,
                FirstName = meeting.OrganizerUser.FirstName,
                LastName = meeting.OrganizerUser.LastName,
                Email = meeting.OrganizerUser.Email
            } : null,
            Status = meeting.Status,
            StatusDisplayName = meeting.Status.ToString(),
            TranscriptFileName = meeting.TranscriptFileName,
            TranscriptFileType = meeting.TranscriptFileType,
            TranscriptUploadedAt = meeting.TranscriptUploadedAt,
            ParticipantCount = meeting.Participants.Count,
            Participants = meeting.Participants.Select(p => new MeetingParticipantDto
            {
                Id = p.Id,
                FullName = p.FullName,
                Email = p.Email,
                Department = p.Department,
                Title = p.Title,
                IsOrganizer = p.IsOrganizer,
                IsRequired = p.IsRequired,
                CreatedAt = p.CreatedAt
            }).ToList(),
            CreatedAt = meeting.CreatedAt,
            UpdatedAt = meeting.UpdatedAt,
            IsArchived = meeting.IsArchived,
            ArchivedAt = meeting.ArchivedAt
        };
    }

    public async Task UpdateMeetingAsync(Guid id, UpdateMeetingRequest request, CancellationToken cancellationToken = default)
    {
        var meeting = await _context.Meetings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (meeting == null)
            throw new NotFoundException(nameof(Meeting), id);

        var isUserAdmin = _currentUserService.Role == UserRole.Admin.ToString();
        if (!isUserAdmin && meeting.OrganizerUserId != _currentUserService.UserId)
            throw new NotFoundException(nameof(Meeting), id);

        if (meeting.IsArchived)
            throw new BadRequestException("Arşivlenmiş toplantılar güncellenemez.");

        var oldValues = new { meeting.Title, meeting.Description, meeting.MeetingDate, meeting.StartTime, meeting.EndTime };

        meeting.Title = request.Title;
        meeting.Description = request.Description;
        meeting.MeetingDate = request.MeetingDate.ToUniversalTime();
        meeting.StartTime = request.StartTime;
        meeting.EndTime = request.EndTime;
        meeting.UpdatedAt = DateTime.UtcNow;

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = _currentUserService.UserId,
            EntityName = nameof(Meeting),
            EntityId = meeting.Id.ToString(),
            Action = "MeetingUpdated",
            CreatedAt = DateTime.UtcNow,
            OldValues = JsonSerializer.Serialize(oldValues),
            NewValues = JsonSerializer.Serialize(new { meeting.Title, meeting.Description, meeting.MeetingDate, meeting.StartTime, meeting.EndTime })
        };
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ArchiveMeetingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var meeting = await _context.Meetings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (meeting == null)
            throw new NotFoundException(nameof(Meeting), id);

        var isUserAdmin = _currentUserService.Role == UserRole.Admin.ToString();
        if (!isUserAdmin && meeting.OrganizerUserId != _currentUserService.UserId)
            throw new NotFoundException(nameof(Meeting), id);

        if (meeting.IsArchived)
            return; // Idempotent

        meeting.StatusBeforeArchive = meeting.Status;
        meeting.IsArchived = true;
        meeting.ArchivedAt = DateTime.UtcNow;
        meeting.Status = MeetingStatus.Archived;
        meeting.UpdatedAt = DateTime.UtcNow;

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = _currentUserService.UserId,
            EntityName = nameof(Meeting),
            EntityId = meeting.Id.ToString(),
            Action = "MeetingArchived",
            CreatedAt = DateTime.UtcNow
        };
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RestoreMeetingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var meeting = await _context.Meetings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (meeting == null)
            throw new NotFoundException(nameof(Meeting), id);

        var isUserAdmin = _currentUserService.Role == UserRole.Admin.ToString();
        if (!isUserAdmin && meeting.OrganizerUserId != _currentUserService.UserId)
            throw new NotFoundException(nameof(Meeting), id);

        if (!meeting.IsArchived)
            return;

        meeting.IsArchived = false;
        meeting.ArchivedAt = null;
        meeting.Status = meeting.StatusBeforeArchive ?? MeetingStatus.Draft;
        meeting.StatusBeforeArchive = null;
        meeting.UpdatedAt = DateTime.UtcNow;

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = _currentUserService.UserId,
            EntityName = nameof(Meeting),
            EntityId = meeting.Id.ToString(),
            Action = "MeetingRestored",
            CreatedAt = DateTime.UtcNow
        };
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
