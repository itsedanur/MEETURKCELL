using TurkcellMeetingAssistant.Application.DTOs.Meetings;

namespace TurkcellMeetingAssistant.Application.Interfaces;

public interface ITranscriptService
{
    Task UpdateTranscriptTextAsync(Guid meetingId, TranscriptTextUpdateRequest request, CancellationToken cancellationToken = default);
    Task ProcessTranscriptFileAsync(Guid meetingId, Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<TranscriptResponseDto> GetTranscriptAsync(Guid meetingId, CancellationToken cancellationToken = default);
    Task DeleteTranscriptAsync(Guid meetingId, CancellationToken cancellationToken = default);
}
