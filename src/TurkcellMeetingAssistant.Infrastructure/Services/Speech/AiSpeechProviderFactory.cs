using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TurkcellMeetingAssistant.Application.Interfaces.Speech;
using TurkcellMeetingAssistant.Application.Models.Speech;

namespace TurkcellMeetingAssistant.Infrastructure.Services.Speech;

public class AiSpeechProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SpeechToTextSettings _settings;

    public AiSpeechProviderFactory(IServiceProvider serviceProvider, IOptions<SpeechToTextSettings> settings)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
    }

    public ISpeechToTextProvider CreateProvider()
    {
        var providerType = _settings.Provider?.ToLowerInvariant();

        return providerType switch
        {
            "openai" => ActivatorUtilities.CreateInstance<OpenAiSpeechToTextProvider>(_serviceProvider),
            _ => ActivatorUtilities.CreateInstance<MockSpeechToTextProvider>(_serviceProvider)
        };
    }
}
