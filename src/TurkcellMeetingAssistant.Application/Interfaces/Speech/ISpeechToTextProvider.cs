namespace TurkcellMeetingAssistant.Application.Interfaces.Speech;

public class SpeechToTextRequest
{
    public Guid RecordingId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
}

public class SpeechToTextResult
{
    public bool IsSuccess { get; set; }
    public string TranscriptText { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? DetectedLanguage { get; set; }
}

public interface ISpeechToTextProvider
{
    string ProviderName { get; }
    Task<SpeechToTextResult> TranscribeAsync(SpeechToTextRequest request, CancellationToken cancellationToken = default);
}
