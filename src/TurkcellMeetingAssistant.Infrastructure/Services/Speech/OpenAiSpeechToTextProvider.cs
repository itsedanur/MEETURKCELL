using System.ClientModel;
using Microsoft.Extensions.Options;
using TurkcellMeetingAssistant.Application.Interfaces.Speech;
using TurkcellMeetingAssistant.Application.Models.AI;
using OpenAI.Audio;

namespace TurkcellMeetingAssistant.Infrastructure.Services.Speech;

public class OpenAiSpeechToTextProvider : ISpeechToTextProvider
{
    private readonly AudioClient _audioClient;

    public string ProviderName => "OpenAI";

    public OpenAiSpeechToTextProvider(IOptions<AiSettings> settings)
    {
        var aiSettings = settings.Value;
        if (string.IsNullOrEmpty(aiSettings.OpenAiApiKey))
        {
            throw new ArgumentException("OpenAI API Key is missing.");
        }

        var modelName = !string.IsNullOrEmpty(aiSettings.OpenAiModelName) && aiSettings.OpenAiModelName.Contains("whisper") 
            ? aiSettings.OpenAiModelName 
            : "whisper-1";

        _audioClient = new AudioClient(modelName, aiSettings.OpenAiApiKey);
    }

    public async Task<SpeechToTextResult> TranscribeAsync(SpeechToTextRequest request, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(request.FilePath))
        {
            return new SpeechToTextResult
            {
                IsSuccess = false,
                ErrorMessage = "File not found"
            };
        }

        try
        {
            var options = new AudioTranscriptionOptions
            {
                Language = !string.IsNullOrEmpty(request.Language) ? request.Language : "tr",
                ResponseFormat = AudioTranscriptionFormat.Verbose
            };

            using var fileStream = File.OpenRead(request.FilePath);
            var response = await _audioClient.TranscribeAudioAsync(fileStream, Path.GetFileName(request.FilePath), options, cancellationToken);
            
            return new SpeechToTextResult
            {
                IsSuccess = true,
                TranscriptText = response.Value.Text,
                DetectedLanguage = response.Value.Language
            };
        }
        catch (Exception ex)
        {
            return new SpeechToTextResult
            {
                IsSuccess = false,
                ErrorMessage = $"OpenAI provider failed: {ex.Message}" // Error message is caught and handled safely by service
            };
        }
    }
}
