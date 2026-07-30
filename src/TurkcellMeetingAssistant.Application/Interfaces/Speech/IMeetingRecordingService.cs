using TurkcellMeetingAssistant.Application.DTOs.Speech;
using TurkcellMeetingAssistant.Domain.Enums;

namespace TurkcellMeetingAssistant.Application.Interfaces.Speech;

public interface IMeetingRecordingService
{
    Task<MeetingRecordingDto> UploadRecordingAsync(UploadRecordingRequestDto request, CancellationToken cancellationToken = default);
    Task<List<MeetingRecordingDto>> GetRecordingsForMeetingAsync(Guid meetingId, CancellationToken cancellationToken = default);
    Task<MeetingRecordingDto?> GetRecordingAsync(Guid meetingId, Guid recordingId, CancellationToken cancellationToken = default);
    
    // Background worker methods
    Task ProcessRecordingAsync(Guid recordingId, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid recordingId, RecordingStatus status, string? errorCode = null, string? errorMessage = null, CancellationToken cancellationToken = default);
}
