using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Cryptography;
using System.Text;
using TurkcellMeetingAssistant.Application.Common.Exceptions;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Summary;
using TurkcellMeetingAssistant.Application.Interfaces;
using TurkcellMeetingAssistant.Application.Services;
using TurkcellMeetingAssistant.Domain.Entities;
using TurkcellMeetingAssistant.Domain.Enums;
using TurkcellMeetingAssistant.Infrastructure.Data;
using TurkcellMeetingAssistant.Infrastructure.Data.Contexts;
using Xunit;

namespace TurkcellMeetingAssistant.UnitTests.Services;

public class MeetingSummaryEditorServiceTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly MeetingSummaryMutationService _mutationService;
    private readonly MeetingSummaryEditorService _sut;
    private readonly Guid _userId;

    public MeetingSummaryEditorServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.UserId).Returns(_userId);
        _mockCurrentUserService.Setup(x => x.Role).Returns(UserRole.User.ToString());

        _mutationService = new MeetingSummaryMutationService(_dbContext, _mockCurrentUserService.Object);
        _sut = new MeetingSummaryEditorService(_dbContext, _mutationService, _mockCurrentUserService.Object);
    }

    private async Task<(Meeting, MeetingSummary)> SeedTestDataAsync(MeetingStatus status = MeetingStatus.AnalysisCompleted, bool isApproved = false)
    {
        var meeting = new Meeting
        {
            Id = Guid.NewGuid(),
            Title = "Test Meeting",
            Status = status,
            OrganizerUserId = _userId,
            MeetingDate = DateTime.UtcNow
        };

        var summary = new MeetingSummary
        {
            Id = Guid.NewGuid(),
            MeetingId = meeting.Id,
            Version = 1,
            IsApproved = isApproved,
            ExecutiveSummary = "Initial Summary",
            ManualRevisionNumber = 0
        };

        _dbContext.Meetings.Add(meeting);
        _dbContext.MeetingSummaries.Add(summary);
        await _dbContext.SaveChangesAsync();

        return (meeting, summary);
    }

    [Fact]
    public async Task UpdateSummaryAsync_ShouldUpdateFieldsAndIncreaseRevision()
    {
        // Arrange
        var (meeting, summary) = await SeedTestDataAsync();
        var request = new UpdateMeetingSummaryRequest { MeetingPurpose = "New Purpose", ExecutiveSummary = "New Exec Summary", ExpectedVersion = 1, ExpectedManualRevisionNumber = 0 };

        // Act
        var result = await _sut.UpdateSummaryAsync(meeting.Id, request, default);

        // Assert
        result.Should().NotBeNull();
        result.ExecutiveSummary.Should().Be("New Exec Summary");
        result.MeetingPurpose.Should().Be("New Purpose");
        
        var updatedSummary = await _dbContext.MeetingSummaries.FindAsync(summary.Id);
        updatedSummary!.ManualRevisionNumber.Should().Be(1);
    }

    [Fact]
    public async Task UpdateSummaryAsync_ShouldThrowConcurrencyException_WhenRevisionMismatch()
    {
        // Arrange
        var (meeting, summary) = await SeedTestDataAsync();
        summary.ManualRevisionNumber = 1;
        await _dbContext.SaveChangesAsync();

        var request = new UpdateMeetingSummaryRequest { ExpectedManualRevisionNumber = 0 };

        // Act & Assert
        await Assert.ThrowsAsync<ConcurrencyException>(() => _sut.UpdateSummaryAsync(meeting.Id, request, default));
    }

    [Fact]
    public async Task AddTopicAsync_ShouldAddTopicToSummary()
    {
        // Arrange
        var (meeting, summary) = await SeedTestDataAsync();
        var request = new CreateMeetingTopicRequest { Title = "Topic 1", SortOrder = 0 };

        // Act
        var result = await _sut.AddTopicAsync(meeting.Id, request, default);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Topic 1");
        
        var topicInDb = await _dbContext.MeetingTopics.FirstOrDefaultAsync(t => t.MeetingSummaryId == summary.Id);
        topicInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateTopicAsync_ShouldUpdateTopicFields()
    {
        // Arrange
        var (meeting, summary) = await SeedTestDataAsync();
        var topic = new MeetingTopic { Id = Guid.NewGuid(), MeetingSummaryId = summary.Id, Title = "Old Title" };
        _dbContext.MeetingTopics.Add(topic);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateMeetingTopicRequest { Title = "New Title" };

        // Act
        var result = await _sut.UpdateTopicAsync(meeting.Id, topic.Id, request, default);

        // Assert
        result.Title.Should().Be("New Title");
    }

    [Fact]
    public async Task DeleteTopicAsync_ShouldSoftDeleteTopic()
    {
        // Arrange
        var (meeting, summary) = await SeedTestDataAsync();
        var topic = new MeetingTopic { Id = Guid.NewGuid(), MeetingSummaryId = summary.Id, Title = "Title" };
        _dbContext.MeetingTopics.Add(topic);
        await _dbContext.SaveChangesAsync();

        // Act
        await _sut.DeleteTopicAsync(meeting.Id, topic.Id, default);

        // Assert
        var deletedTopic = await _dbContext.MeetingTopics.IgnoreQueryFilters().FirstAsync(t => t.Id == topic.Id);
        deletedTopic.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task AddDecisionAsync_ShouldAddDecisionToSummary()
    {
        // Arrange
        var (meeting, summary) = await SeedTestDataAsync();
        var request = new CreateMeetingDecisionRequest { Description = "Dec 1", ConfidenceScore = 0.9m };

        // Act
        var result = await _sut.AddDecisionAsync(meeting.Id, request, default);

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().Be("Dec 1");
    }

    [Fact]
    public async Task ApproveSummaryAsync_ShouldSetApprovedHashAndStatus()
    {
        // Arrange
        var (meeting, summary) = await SeedTestDataAsync();
        var user = new User { Id = _userId, Email = "test@test.com", FirstName = "Test", LastName = "User" };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        
        var request = new ApproveMeetingSummaryRequest { Confirmation = true, ExpectedVersion = 1, ExpectedManualRevisionNumber = 0 };

        // Act
        var result = await _sut.ApproveSummaryAsync(meeting.Id, request, default);

        // Assert
        result.IsApproved.Should().BeTrue();
        
        var updatedMeeting = await _dbContext.Meetings.FindAsync(meeting.Id);
        updatedMeeting!.Status.Should().Be(MeetingStatus.Approved);
        
        var updatedSummary = await _dbContext.MeetingSummaries.FindAsync(summary.Id);
        updatedSummary!.IsApproved.Should().BeTrue();
        updatedSummary.ApprovedContentHash.Should().NotBeNullOrEmpty();
    }
}
