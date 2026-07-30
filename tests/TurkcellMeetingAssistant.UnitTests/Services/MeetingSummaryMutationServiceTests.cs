using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Cryptography;
using System.Text;
using TurkcellMeetingAssistant.Application.Common.Exceptions;
using TurkcellMeetingAssistant.Application.Interfaces;
using TurkcellMeetingAssistant.Application.Services;
using TurkcellMeetingAssistant.Domain.Entities;
using TurkcellMeetingAssistant.Domain.Enums;
using TurkcellMeetingAssistant.Infrastructure.Data.Contexts;
using Xunit;

namespace TurkcellMeetingAssistant.UnitTests.Services;

public class MeetingSummaryMutationServiceTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly MeetingSummaryMutationService _sut;
    private readonly Guid _userId;

    public MeetingSummaryMutationServiceTests()
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

        _sut = new MeetingSummaryMutationService(_dbContext, _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task MutateSummary_ShouldRevokeApproval_WhenCalled()
    {
        // Arrange
        var meeting = new Meeting { Id = Guid.NewGuid(), Title = "M1", OrganizerUserId = _userId, Status = MeetingStatus.Approved };
        var summary = new MeetingSummary { Id = Guid.NewGuid(), MeetingId = meeting.Id, Version = 1, IsApproved = true, ApprovedContentHash = "hash" };
        
        _dbContext.Meetings.Add(meeting);
        _dbContext.MeetingSummaries.Add(summary);
        await _dbContext.SaveChangesAsync();

        var (m, s) = await _sut.GetActiveSummaryForMutationAsync(meeting.Id, 1, 0, false, default);

        // Act
        _sut.MutateSummary(m, s, "UpdateTopic");

        // Assert
        s.IsApproved.Should().BeFalse();
        s.ApprovedContentHash.Should().BeNull();
        m.Status.Should().Be(MeetingStatus.WaitingForApproval);
    }
}
