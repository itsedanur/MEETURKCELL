using TurkcellMeetingAssistant.Application.DTOs.Meetings;

namespace TurkcellMeetingAssistant.Application.Interfaces;

public interface IMeetingParticipantService
{
    Task<Guid> AddParticipantAsync(Guid meetingId, CreateParticipantRequest request, CancellationToken cancellationToken = default);
    Task UpdateParticipantAsync(Guid meetingId, Guid participantId, UpdateParticipantRequest request, CancellationToken cancellationToken = default);
    Task RemoveParticipantAsync(Guid meetingId, Guid participantId, CancellationToken cancellationToken = default);
}
