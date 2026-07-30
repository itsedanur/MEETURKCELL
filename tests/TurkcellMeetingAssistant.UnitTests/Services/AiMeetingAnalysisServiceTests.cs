using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Analysis;
using TurkcellMeetingAssistant.Application.Interfaces.AI;
using TurkcellMeetingAssistant.Infrastructure.Services;
using TurkcellMeetingAssistant.Application.DTOs.Meetings;
using Xunit;

namespace TurkcellMeetingAssistant.UnitTests.Services;

public class AiMeetingAnalysisServiceTests
{
    private readonly Mock<IAiProvider> _mockAiProvider;
    private readonly Mock<IPromptService> _mockPromptService;
    private readonly Mock<ILogger<AiMeetingAnalysisService>> _mockLogger;
    private readonly AiMeetingAnalysisService _service;

    public AiMeetingAnalysisServiceTests()
    {
        _mockAiProvider = new Mock<IAiProvider>();
        _mockPromptService = new Mock<IPromptService>();
        _mockLogger = new Mock<ILogger<AiMeetingAnalysisService>>();

        _service = new AiMeetingAnalysisService(
            _mockAiProvider.Object,
            _mockPromptService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldReturnParsedResult_WhenAiReturnsValidJson()
    {
        // Arrange
        var input = new MeetingAnalysisInput
        {
            MeetingId = Guid.NewGuid(),
            MeetingTitle = "Test Meeting",
            TranscriptText = "Test transcript",
            Participants = new List<AnalysisParticipantDto>
            {
                new AnalysisParticipantDto { FullName = "John Doe", Email = "john@example.com" }
            }
        };

        _mockPromptService.Setup(p => p.GetPromptTemplateAsync("MeetingAnalysis", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Template Content");

        _mockPromptService.Setup(p => p.BuildPrompt(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Returns("Rendered Prompt");

        _mockAiProvider.Setup(a => a.ProviderName).Returns("Mock");

        var fakeResult = new MeetingAnalysisResult
        {
            ExecutiveSummary = "Test Summary"
        };
        var fakeJson = JsonSerializer.Serialize(fakeResult, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        _mockAiProvider.Setup(a => a.GenerateTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeJson);

        // Act
        var result = await _service.AnalyzeAsync(input, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Summary", result.ExecutiveSummary);
        
        _mockPromptService.Verify(p => p.GetPromptTemplateAsync("MeetingAnalysis", It.IsAny<CancellationToken>()), Times.Once);
        _mockAiProvider.Verify(a => a.GenerateTextAsync(It.IsAny<string>(), "Rendered Prompt", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldThrowException_WhenAiReturnsInvalidJson()
    {
        // Arrange
        var input = new MeetingAnalysisInput
        {
            MeetingId = Guid.NewGuid(),
            MeetingTitle = "Test",
            TranscriptText = "Text",
            Participants = new List<AnalysisParticipantDto>()
        };

        _mockPromptService.Setup(p => p.GetPromptTemplateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("");
        _mockPromptService.Setup(p => p.BuildPrompt(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>())).Returns("");
        _mockAiProvider.Setup(a => a.ProviderName).Returns("Mock");

        _mockAiProvider.Setup(a => a.GenerateTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("invalid json block");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AnalyzeAsync(input, CancellationToken.None));
    }
}
