using TurkcellMeetingAssistant.Application.Interfaces.Speech;

namespace TurkcellMeetingAssistant.Infrastructure.Services.Speech;

public class MockSpeechToTextProvider : ISpeechToTextProvider
{
    public string ProviderName => "Mock";

    public async Task<SpeechToTextResult> TranscribeAsync(SpeechToTextRequest request, CancellationToken cancellationToken = default)
    {
        // Simulate delay
        await Task.Delay(2000, cancellationToken);

        return new SpeechToTextResult
        {
            IsSuccess = true,
            TranscriptText = "Bu metin Mock Speech to Text servisi tarafından oluşturulmuştur. Toplantıda alınan kararlar ve aksiyonlar burada yer alabilir.",
            DetectedLanguage = "tr"
        };
    }
}
