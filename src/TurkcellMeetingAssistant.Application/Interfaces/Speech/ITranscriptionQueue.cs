namespace TurkcellMeetingAssistant.Application.Interfaces.Speech;

public class TranscriptionJob
{
    public Guid RecordingId { get; set; }
    public Guid MeetingId { get; set; }
}

public interface ITranscriptionQueue
{
    ValueTask QueueJobAsync(TranscriptionJob job, CancellationToken cancellationToken = default);
    ValueTask<TranscriptionJob> DequeueAsync(CancellationToken cancellationToken = default);
}
