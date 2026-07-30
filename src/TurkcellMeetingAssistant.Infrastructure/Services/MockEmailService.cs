using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TurkcellMeetingAssistant.Application.Common.Models;
using TurkcellMeetingAssistant.Application.Interfaces;

namespace TurkcellMeetingAssistant.Infrastructure.Services;

public class MockEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<MockEmailService> _logger;

    public MockEmailService(IOptions<EmailSettings> options, ILogger<MockEmailService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Simulating email send for {ToCount} recipients. Subject: {Subject}", message.To.Count, message.Subject);

        // Simulate network delay
        await Task.Delay(Random.Shared.Next(100, 500), cancellationToken);

        if (_settings.MockFailureMode)
        {
            var failureChance = _settings.MockFailureRate > 0 ? _settings.MockFailureRate : 50;
            if (Random.Shared.Next(0, 100) < failureChance)
            {
                _logger.LogWarning("Mock failure triggered for email send.");
                return new EmailSendResult
                {
                    IsSuccessful = false,
                    Provider = "Mock",
                    ErrorCode = "MOCK_FAILURE_01",
                    ErrorMessage = "Mock failure triggered by configuration."
                };
            }
        }

        return new EmailSendResult
        {
            IsSuccessful = true,
            Provider = "Mock",
            ProviderMessageId = $"mock-msg-{Guid.NewGuid():N}",
            SentAt = DateTime.UtcNow
        };
    }
}
