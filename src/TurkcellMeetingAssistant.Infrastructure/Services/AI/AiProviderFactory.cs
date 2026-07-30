using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TurkcellMeetingAssistant.Application.Interfaces.AI;
using TurkcellMeetingAssistant.Application.Models.AI;

namespace TurkcellMeetingAssistant.Infrastructure.Services.AI;

public class AiProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AiSettings _settings;

    public AiProviderFactory(IServiceProvider serviceProvider, IOptions<AiSettings> settings)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
    }

    public IAiProvider CreateProvider()
    {
        var providerType = _settings.Provider?.ToLowerInvariant();

        return providerType switch
        {
            "openai" => ActivatorUtilities.CreateInstance<OpenAiProvider>(_serviceProvider),
            "azureopenai" => ActivatorUtilities.CreateInstance<AzureOpenAiProvider>(_serviceProvider),
            _ => ActivatorUtilities.CreateInstance<MockAiProvider>(_serviceProvider)
        };
    }
}
