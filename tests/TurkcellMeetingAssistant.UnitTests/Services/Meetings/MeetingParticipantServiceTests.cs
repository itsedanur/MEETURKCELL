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

public class MeetingParticipantServiceTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly MeetingParticipantService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public MeetingParticipantServiceTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _contextMock.Setup(x => x.AuditLogs).ReturnsDbSet(new List<AuditLog>());
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Role).Returns(UserRole.User.ToString());

        _sut = new MeetingParticipantService(_contextMock.Object, _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task AddParticipantAsync_ShouldReturnNewParticipantId_WhenValidRequest()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var meeting = new Meeting { Id = meetingId, OrganizerUserId = _userId };
        _contextMock.Setup(x => x.Meetings).ReturnsDbSet(new List<Meeting> { meeting });
        _contextMock.Setup(x => x.MeetingParticipants).ReturnsDbSet(new List<MeetingParticipant>());

        var request = new CreateParticipantRequest { Email = "test@example.com", FullName = "Test User" };

        // Act
        var result = await _sut.AddParticipantAsync(meetingId, request);

        // Assert
        result.Should().NotBeEmpty();
        _contextMock.Verify(x => x.MeetingParticipants.Add(It.IsAny<MeetingParticipant>()), Times.Once);
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddParticipantAsync_ShouldThrowBadRequest_WhenEmailAlreadyExists()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var meeting = new Meeting { Id = meetingId, OrganizerUserId = _userId };
        _contextMock.Setup(x => x.Meetings).ReturnsDbSet(new List<Meeting> { meeting });
        
        var existingParticipant = new MeetingParticipant { MeetingId = meetingId, Email = "test@example.com" };
        _contextMock.Setup(x => x.MeetingParticipants).ReturnsDbSet(new List<MeetingParticipant> { existingParticipant });

        var request = new CreateParticipantRequest { Email = "test@example.com", FullName = "Test User" };

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddParticipantAsync(meetingId, request));
    }

    [Fact]
    public async Task AddParticipantAsync_ShouldThrowNotFound_WhenUserIsNotOrganizer()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var meeting = new Meeting { Id = meetingId, OrganizerUserId = Guid.NewGuid() }; // Different user
        _contextMock.Setup(x => x.Meetings).ReturnsDbSet(new List<Meeting> { meeting });

        var request = new CreateParticipantRequest { Email = "test@example.com", FullName = "Test User" };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.AddParticipantAsync(meetingId, request));
    }

    [Fact]
    public async Task UpdateParticipantAsync_ShouldUpdate_WhenValidRequest()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var meeting = new Meeting { Id = meetingId, OrganizerUserId = _userId };
        _contextMock.Setup(x => x.Meetings).ReturnsDbSet(new List<Meeting> { meeting });

        var participant = new MeetingParticipant { Id = participantId, MeetingId = meetingId, Email = "old@example.com" };
        _contextMock.Setup(x => x.MeetingParticipants).ReturnsDbSet(new List<MeetingParticipant> { participant });

        var request = new UpdateParticipantRequest { Email = "new@example.com", FullName = "New Name" };

        // Act
        await _sut.UpdateParticipantAsync(meetingId, participantId, request);

        // Assert
        participant.Email.Should().Be("new@example.com");
        participant.FullName.Should().Be("New Name");
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveParticipantAsync_ShouldRemove_WhenValidRequest()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var meeting = new Meeting { Id = meetingId, OrganizerUserId = _userId };
        _contextMock.Setup(x => x.Meetings).ReturnsDbSet(new List<Meeting> { meeting });

        var participant = new MeetingParticipant { Id = participantId, MeetingId = meetingId };
        _contextMock.Setup(x => x.MeetingParticipants).ReturnsDbSet(new List<MeetingParticipant> { participant });

        // Act
        await _sut.RemoveParticipantAsync(meetingId, participantId);

        // Assert
        _contextMock.Verify(x => x.MeetingParticipants.Remove(participant), Times.Once);
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
