using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TurkcellMeetingAssistant.Application.Common.Exceptions;
using TurkcellMeetingAssistant.Application.Common.Models;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Summary;
using TurkcellMeetingAssistant.Application.Interfaces;
using TurkcellMeetingAssistant.Application.Services;
using TurkcellMeetingAssistant.Domain.Entities;
using TurkcellMeetingAssistant.Domain.Enums;
using TurkcellMeetingAssistant.Infrastructure.Data;
using TurkcellMeetingAssistant.Infrastructure.Data.Contexts;
using Xunit;

namespace TurkcellMeetingAssistant.UnitTests.Services;

public class MeetingActionItemServiceTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly MeetingSummaryMutationService _mutationService;
    private readonly MeetingActionItemService _sut;
    private readonly Guid _userId;

    public MeetingActionItemServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.UserId).Returns(_userId);
        _mockCurrentUserService.Setup(x => x.Role).Returns(UserRole.Admin.ToString());

        _mutationService = new MeetingSummaryMutationService(_dbContext, _mockCurrentUserService.Object);
        _sut = new MeetingActionItemService(_dbContext, _mutationService, _mockCurrentUserService.Object);
    }

    private async Task<(Meeting, MeetingSummary, MeetingParticipant)> SeedTestDataAsync()
    {
        var meeting = new Meeting
        {
            Id = Guid.NewGuid(),
            Title = "Test Meeting",
            Status = MeetingStatus.AnalysisCompleted,
            OrganizerUserId = _userId,
            MeetingDate = DateTime.UtcNow
        };

        var summary = new MeetingSummary
        {
            Id = Guid.NewGuid(),
            MeetingId = meeting.Id,
            Version = 1,
            IsApproved = false,
            ExecutiveSummary = "Initial Summary",
            ManualRevisionNumber = 0
        };

        var participant = new MeetingParticipant
        {
            Id = Guid.NewGuid(),
            MeetingId = meeting.Id,
            FullName = "John Doe",
            Email = "john.doe@test.com"
        };

        _dbContext.Meetings.Add(meeting);
        _dbContext.MeetingSummaries.Add(summary);
        _dbContext.MeetingParticipants.Add(participant);
        await _dbContext.SaveChangesAsync();

        return (meeting, summary, participant);
    }

    [Fact]
    public async Task AddActionItemAsync_ShouldAddActionItemToSummary()
    {
        // Arrange
        var (meeting, summary, participant) = await SeedTestDataAsync();
        var request = new CreateActionItemRequest 
        { 
            Description = "Action 1", 
            AssignedParticipantId = participant.Id,
            Priority = ActionPriority.High,
            Status = ActionItemStatus.Open
        };

        // Act
        var result = await _sut.AddActionItemAsync(meeting.Id, request, default);

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().Be("Action 1");
        result.OwnerName.Should().Be("John Doe");
        
        var actionInDb = await _dbContext.ActionItems.FirstOrDefaultAsync(a => a.MeetingSummaryId == summary.Id);
        actionInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateActionItemStatusAsync_ShouldUpdateStatus()
    {
        // Arrange
        var (meeting, summary, _) = await SeedTestDataAsync();
        var action = new ActionItem
        {
            Id = Guid.NewGuid(),
            MeetingId = meeting.Id,
            MeetingSummaryId = summary.Id,
            Description = "Action",
            Status = ActionItemStatus.Open
        };
        _dbContext.ActionItems.Add(action);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateActionItemStatusRequest { Status = ActionItemStatus.InProgress };

        // Act
        var result = await _sut.UpdateActionItemStatusAsync(meeting.Id, action.Id, request, default);

        // Assert
        result.Status.Should().Be(ActionItemStatus.InProgress);
        
        var updatedAction = await _dbContext.ActionItems.FindAsync(action.Id);
        updatedAction!.Status.Should().Be(ActionItemStatus.InProgress);
    }

    [Fact]
    public async Task UpdateActionItemStatusAsync_ShouldSetCompletedBy_WhenStatusCompleted()
    {
        // Arrange
        var (meeting, summary, _) = await SeedTestDataAsync();
        var action = new ActionItem
        {
            Id = Guid.NewGuid(),
            MeetingId = meeting.Id,
            MeetingSummaryId = summary.Id,
            Description = "Action",
            Status = ActionItemStatus.InProgress
        };
        _dbContext.ActionItems.Add(action);
        await _dbContext.SaveChangesAsync();

        var request = new UpdateActionItemStatusRequest { Status = ActionItemStatus.Completed };

        // Act
        await _sut.UpdateActionItemStatusAsync(meeting.Id, action.Id, request, default);

        // Assert
        var updatedAction = await _dbContext.ActionItems.FindAsync(action.Id);
        updatedAction!.CompletedByUserId.Should().Be(_userId);
        updatedAction.CompletedAt.Should().NotBeNull();
    }
}
