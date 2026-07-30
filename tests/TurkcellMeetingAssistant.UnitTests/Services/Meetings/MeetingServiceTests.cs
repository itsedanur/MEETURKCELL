using FluentAssertions;
using Moq;
using Moq.EntityFrameworkCore;
using TurkcellMeetingAssistant.Application.Common.Exceptions;
using TurkcellMeetingAssistant.Application.DTOs.Meetings;
using TurkcellMeetingAssistant.Application.Interfaces;
using TurkcellMeetingAssistant.Application.Services;
using TurkcellMeetingAssistant.Domain.Entities;
using TurkcellMeetingAssistant.Domain.Enums;
using Xunit;

namespace TurkcellMeetingAssistant.UnitTests.Services.Meetings;

public class MeetingServiceTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly MeetingService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public MeetingServiceTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _contextMock.Setup(x => x.AuditLogs).ReturnsDbSet(new List<AuditLog>());
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Role).Returns(UserRole.User.ToString());

        _sut = new MeetingService(_contextMock.Object, _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task CreateMeetingAsync_ShouldReturnNewMeetingId_WhenValidRequest()
    {
        // Arrange
        var request = new CreateMeetingRequest
        {
            Title = "Test Meeting",
            MeetingDate = DateTime.UtcNow
        };
        _contextMock.Setup(x => x.Meetings.Add(It.IsAny<Meeting>()));
        _contextMock.Setup(x => x.AuditLogs.Add(It.IsAny<AuditLog>()));

        // Act
        var result = await _sut.CreateMeetingAsync(request);

        // Assert
        result.Should().NotBeEmpty();
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateMeetingAsync_ShouldThrowUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.Empty);
        var request = new CreateMeetingRequest { Title = "Test Meeting" };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.CreateMeetingAsync(request));
    }

    [Fact]
    public async Task GetMeetingByIdAsync_ShouldReturnMeeting_WhenExistsAndUserIsOrganizer()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var meetings = new List<Meeting>
        {
            new Meeting { Id = meetingId, Title = "Test", OrganizerUserId = _userId }
        };
        
        _contextMock.Setup(x => x.Meetings).ReturnsDbSet(meetings);

        // Act
        var result = await _sut.GetMeetingByIdAsync(meetingId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(meetingId);
        result.Title.Should().Be("Test");
    }

    [Fact]
    public async Task GetMeetingByIdAsync_ShouldThrowNotFound_WhenMeetingDoesNotExist()
    {
        // Arrange
        _contextMock.Setup(x => x.Meetings).ReturnsDbSet(new List<Meeting>());

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetMeetingByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetMeetingByIdAsync_ShouldThrowNotFound_WhenUserIsNotOrganizerAndNotAdmin()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var meetings = new List<Meeting>
        {
            new Meeting { Id = meetingId, Title = "Test", OrganizerUserId = Guid.NewGuid() }
        };
        
        _contextMock.Setup(x => x.Meetings).ReturnsDbSet(meetings);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetMeetingByIdAsync(meetingId));
    }

    [Fact]
    public async Task UpdateMeetingAsync_ShouldUpdate_WhenValidRequest()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var meeting = new Meeting { Id = meetingId, Title = "Old", OrganizerUserId = _userId };
        var meetings = new List<Meeting> { meeting };
        _contextMock.Setup(x => x.Meetings).ReturnsDbSet(meetings);

        var request = new UpdateMeetingRequest { Title = "New Title", MeetingDate = DateTime.UtcNow };

        // Act
        await _sut.UpdateMeetingAsync(meetingId, request);

        // Assert
        meeting.Title.Should().Be("New Title");
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateMeetingAsync_ShouldThrowBadRequest_WhenMeetingIsArchived()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var meeting = new Meeting { Id = meetingId, OrganizerUserId = _userId, IsArchived = true };
        _contextMock.Setup(x => x.Meetings).ReturnsDbSet(new List<Meeting> { meeting });

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.UpdateMeetingAsync(meetingId, new UpdateMeetingRequest()));
    }

    [Fact]
    public async Task ArchiveMeetingAsync_ShouldSetIsArchivedToTrue()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var meeting = new Meeting { Id = meetingId, OrganizerUserId = _userId, Status = MeetingStatus.Draft };
        _contextMock.Setup(x => x.Meetings).ReturnsDbSet(new List<Meeting> { meeting });

        // Act
        await _sut.ArchiveMeetingAsync(meetingId);

        // Assert
        meeting.IsArchived.Should().BeTrue();
        meeting.Status.Should().Be(MeetingStatus.Archived);
        meeting.StatusBeforeArchive.Should().Be(MeetingStatus.Draft);
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RestoreMeetingAsync_ShouldSetIsArchivedToFalse()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var meeting = new Meeting { Id = meetingId, OrganizerUserId = _userId, IsArchived = true, Status = MeetingStatus.Archived, StatusBeforeArchive = MeetingStatus.ReadyForAnalysis };
        _contextMock.Setup(x => x.Meetings).ReturnsDbSet(new List<Meeting> { meeting });

        // Act
        await _sut.RestoreMeetingAsync(meetingId);

        // Assert
        meeting.IsArchived.Should().BeFalse();
        meeting.Status.Should().Be(MeetingStatus.ReadyForAnalysis);
        meeting.StatusBeforeArchive.Should().BeNull();
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMeetingsAsync_ShouldReturnPagedResult()
    {
        // Arrange
        var meetings = new List<Meeting>
        {
            new Meeting { Id = Guid.NewGuid(), Title = "A", OrganizerUserId = _userId },
            new Meeting { Id = Guid.NewGuid(), Title = "B", OrganizerUserId = _userId }
        };
        _contextMock.Setup(x => x.Meetings).ReturnsDbSet(meetings);

        // Act
        var result = await _sut.GetMeetingsAsync(1, 10, null, null, null, null, false);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }
}
