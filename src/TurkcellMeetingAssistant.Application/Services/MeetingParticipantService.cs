using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TurkcellMeetingAssistant.Application.Common.Exceptions;
using TurkcellMeetingAssistant.Application.DTOs.Meetings;
using TurkcellMeetingAssistant.Application.Interfaces;
using TurkcellMeetingAssistant.Domain.Entities;
using TurkcellMeetingAssistant.Domain.Enums;

namespace TurkcellMeetingAssistant.Application.Services;

public class MeetingParticipantService : IMeetingParticipantService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public MeetingParticipantService(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> AddParticipantAsync(Guid meetingId, CreateParticipantRequest request, CancellationToken cancellationToken = default)
    {
        var meeting = await _context.Meetings.FirstOrDefaultAsync(x => x.Id == meetingId, cancellationToken);
        if (meeting == null)
            throw new NotFoundException(nameof(Meeting), meetingId);

        var isUserAdmin = _currentUserService.Role == UserRole.Admin.ToString();
        if (!isUserAdmin && meeting.OrganizerUserId != _currentUserService.UserId)
            throw new NotFoundException(nameof(Meeting), meetingId);

        if (meeting.IsArchived)
            throw new BadRequestException("Arşivlenmiş toplantıya katılımcı eklenemez.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existingParticipant = await _context.MeetingParticipants
            .FirstOrDefaultAsync(x => x.MeetingId == meetingId && x.Email.ToLower() == normalizedEmail, cancellationToken);
        
        if (existingParticipant != null)
            throw new BadRequestException("Bu e-posta adresi bu toplantıya zaten eklenmiş.");

        var participant = new MeetingParticipant
        {
            Id = Guid.NewGuid(),
            MeetingId = meetingId,
            FullName = request.FullName,
            Email = request.Email, // store original, db unique index might enforce case sensitive or not, but we check manually anyway
            Department = request.Department,
            Title = request.Title,
            IsOrganizer = request.IsOrganizer,
            IsRequired = request.IsRequired,
            CreatedAt = DateTime.UtcNow
        };

        _context.MeetingParticipants.Add(participant);

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = _currentUserService.UserId,
            EntityName = nameof(MeetingParticipant),
            EntityId = participant.Id.ToString(),
            Action = "ParticipantAdded",
            CreatedAt = DateTime.UtcNow,
            NewValues = JsonSerializer.Serialize(new { participant.Email, participant.FullName })
        };
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);

        return participant.Id;
    }

    public async Task UpdateParticipantAsync(Guid meetingId, Guid participantId, UpdateParticipantRequest request, CancellationToken cancellationToken = default)
    {
        var meeting = await _context.Meetings.FirstOrDefaultAsync(x => x.Id == meetingId, cancellationToken);
        if (meeting == null)
            throw new NotFoundException(nameof(Meeting), meetingId);

        var isUserAdmin = _currentUserService.Role == UserRole.Admin.ToString();
        if (!isUserAdmin && meeting.OrganizerUserId != _currentUserService.UserId)
            throw new NotFoundException(nameof(Meeting), meetingId);

        if (meeting.IsArchived)
            throw new BadRequestException("Arşivlenmiş toplantıda katılımcı güncellenemez.");

        var participant = await _context.MeetingParticipants
            .FirstOrDefaultAsync(x => x.Id == participantId && x.MeetingId == meetingId, cancellationToken);
            
        if (participant == null)
            throw new NotFoundException(nameof(MeetingParticipant), participantId);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (participant.Email.ToLowerInvariant() != normalizedEmail)
        {
            var existingParticipant = await _context.MeetingParticipants
                .FirstOrDefaultAsync(x => x.MeetingId == meetingId && x.Email.ToLower() == normalizedEmail, cancellationToken);
            
            if (existingParticipant != null)
                throw new BadRequestException("Bu e-posta adresi bu toplantıya zaten eklenmiş.");
        }

        var oldValues = new { participant.FullName, participant.Email, participant.Department, participant.Title, participant.IsOrganizer, participant.IsRequired };

        participant.FullName = request.FullName;
        participant.Email = request.Email;
        participant.Department = request.Department;
        participant.Title = request.Title;
        participant.IsOrganizer = request.IsOrganizer;
        participant.IsRequired = request.IsRequired;
        participant.UpdatedAt = DateTime.UtcNow;

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = _currentUserService.UserId,
            EntityName = nameof(MeetingParticipant),
            EntityId = participant.Id.ToString(),
            Action = "ParticipantUpdated",
            CreatedAt = DateTime.UtcNow,
            OldValues = JsonSerializer.Serialize(oldValues),
            NewValues = JsonSerializer.Serialize(new { participant.FullName, participant.Email })
        };
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveParticipantAsync(Guid meetingId, Guid participantId, CancellationToken cancellationToken = default)
    {
        var meeting = await _context.Meetings.FirstOrDefaultAsync(x => x.Id == meetingId, cancellationToken);
        if (meeting == null)
            throw new NotFoundException(nameof(Meeting), meetingId);

        var isUserAdmin = _currentUserService.Role == UserRole.Admin.ToString();
        if (!isUserAdmin && meeting.OrganizerUserId != _currentUserService.UserId)
            throw new NotFoundException(nameof(Meeting), meetingId);

        if (meeting.IsArchived)
            throw new BadRequestException("Arşivlenmiş toplantıdan katılımcı silinemez.");

        var participant = await _context.MeetingParticipants
            .FirstOrDefaultAsync(x => x.Id == participantId && x.MeetingId == meetingId, cancellationToken);
            
        if (participant == null)
            throw new NotFoundException(nameof(MeetingParticipant), participantId);

        _context.MeetingParticipants.Remove(participant);

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = _currentUserService.UserId,
            EntityName = nameof(MeetingParticipant),
            EntityId = participant.Id.ToString(),
            Action = "ParticipantRemoved",
            CreatedAt = DateTime.UtcNow
        };
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
