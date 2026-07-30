using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TurkcellMeetingAssistant.Application.Common.Models;
using TurkcellMeetingAssistant.Infrastructure.Services;
using Xunit;

namespace TurkcellMeetingAssistant.UnitTests.Services;

public class MockEmailServiceTests
{
    [Fact]
    public async Task SendAsync_ShouldReturnSuccess_WhenMockFailureModeIsOff()
    {
        // Arrange
        var settings = Options.Create(new EmailSettings { MockFailureMode = false });
        var logger = new Mock<ILogger<MockEmailService>>();
        var sut = new MockEmailService(settings, logger.Object);
        var message = new EmailMessage { Subject = "Test" };
        message.To.Add(new EmailAddressModel { Email = "test@test.com" });

        // Act
        var result = await sut.SendAsync(message, default);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.ProviderMessageId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SendAsync_ShouldReturnFailure_WhenMockFailureModeIsOnAndRateIs100()
    {
        // Arrange
        var settings = Options.Create(new EmailSettings { MockFailureMode = true, MockFailureRate = 100 });
        var logger = new Mock<ILogger<MockEmailService>>();
        var sut = new MockEmailService(settings, logger.Object);
        var message = new EmailMessage { Subject = "Test" };
        message.To.Add(new EmailAddressModel { Email = "test@test.com" });

        // Act
        var result = await sut.SendAsync(message, default);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.ErrorCode.Should().Be("MOCK_FAILURE_01");
    }
}
