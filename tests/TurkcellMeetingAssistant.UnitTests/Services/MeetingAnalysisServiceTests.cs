using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TurkcellMeetingAssistant.Application.Common.Exceptions;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Analysis;
using TurkcellMeetingAssistant.Application.Interfaces;
using TurkcellMeetingAssistant.Application.Services;
using TurkcellMeetingAssistant.Domain.Entities;
using TurkcellMeetingAssistant.Domain.Enums;
using TurkcellMeetingAssistant.Infrastructure.Data.Contexts;
using Xunit;

namespace TurkcellMeetingAssistant.UnitTests.Services;

public class MeetingAnalysisServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IAiMeetingAnalysisService> _mockAiAnalysisService;
    private readonly Mock<IValidator<MeetingAnalysisResult>> _mockValidator;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<MeetingAnalysisService>> _mockLogger;
    private readonly MeetingAnalysisService _sut;

    public MeetingAnalysisServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        _context = new ApplicationDbContext(options);
        
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockAiAnalysisService = new Mock<IAiMeetingAnalysisService>();
        _mockValidator = new Mock<IValidator<MeetingAnalysisResult>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<MeetingAnalysisService>>();

        _mockCurrentUserService.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _mockCurrentUserService.Setup(c => c.Role).Returns(UserRole.User.ToString());
        
        var mockConfSection = new Mock<IConfigurationSection>();
        mockConfSection.Setup(s => s.Value).Returns("50");
        _mockConfiguration.Setup(c => c.GetSection("MeetingAnalysis:MinimumTranscriptLength")).Returns(mockConfSection.Object);
        _mockConfiguration.Setup(c => c["MeetingAnalysis:Provider"]).Returns("Mock");
        _mockConfiguration.Setup(c => c["MeetingAnalysis:PromptVersion"]).Returns("v1");

        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<MeetingAnalysisResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _sut = new MeetingAnalysisService(
            _context,
            _mockCurrentUserService.Object,
            _mockAiAnalysisService.Object,
            _mockValidator.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task AnalyzeMeetingAsync_WhenTranscriptTooShort_ThrowsTranscriptTooShortException()
    {
        // Arrange
        var meeting = new Meeting
        {
            Id = Guid.NewGuid(),
            Title = "Test Meeting",
            TranscriptText = "Kısa",
            OrganizerUserId = _mockCurrentUserService.Object.UserId!.Value,
            Status = MeetingStatus.ReadyForAnalysis
        };
        _context.Meetings.Add(meeting);
        await _context.SaveChangesAsync();

        // Act & Assert
        await FluentActions.Invoking(() => _sut.AnalyzeMeetingAsync(meeting.Id, CancellationToken.None))
            .Should().ThrowAsync<TranscriptTooShortException>();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
