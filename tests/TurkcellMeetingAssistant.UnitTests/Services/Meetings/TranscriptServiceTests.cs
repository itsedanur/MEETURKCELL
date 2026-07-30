using FluentAssertions;
using Moq;
using Moq.EntityFrameworkCore;
using System.Text;
using TurkcellMeetingAssistant.Application.Common.Exceptions;
using TurkcellMeetingAssistant.Application.DTOs.Meetings;
using TurkcellMeetingAssistant.Application.Interfaces;
using TurkcellMeetingAssistant.Application.Services;
using TurkcellMeetingAssistant.Domain.Entities;
using TurkcellMeetingAssistant.Domain.Enums;
using Xunit;

namespace TurkcellMeetingAssistant.UnitTests.Services.Meetings;

public class TranscriptServiceTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly TranscriptService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public TranscriptServiceTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _contextMock.Setup(x => x.AuditLogs).ReturnsDbSet(new List<AuditLog>());
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(x => x.Role).Returns(UserRole.User.ToString());

        _sut = new TranscriptService(_contextMock.Object, _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task UpdateTranscriptTextAsync_ShouldUpdate_WhenValidRequest()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var meeting = new Meeting { Id = meetingId, OrganizerUserId = _userId, Status = MeetingStatus.Draft };
        _contextMock.Setup(x => x.Meetings).ReturnsDbSet(new List<Meeting> { meeting });

        var request = new TranscriptTextUpdateRequest { TranscriptText = "This is a valid long enough transcript text for testing." };

        // Act
        await _sut.UpdateTranscriptTextAsync(meetingId, request);

        // Assert
        meeting.TranscriptText.Should().Be(request.TranscriptText);
        meeting.Status.Should().Be(MeetingStatus.ReadyForAnalysis);
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessTranscriptFileAsync_ShouldUpdate_WhenValidTxtFile()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var meeting = new Meeting { Id = meetingId, OrganizerUserId = _userId };
        _contextMock.Setup(x => x.Meetings).ReturnsDbSet(new List<Meeting> { meeting });

        var content = "This is a valid long enough transcript text for testing.";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        await _sut.ProcessTranscriptFileAsync(meetingId, stream, "test.txt", "text/plain");

        // Assert
        meeting.TranscriptText.Should().Be(content);
        meeting.TranscriptFileName.Should().Be("test.txt");
        meeting.Status.Should().Be(MeetingStatus.ReadyForAnalysis);
    }

    [Fact]
    public async Task ProcessTranscriptFileAsync_ShouldParseVttAndUpdate_WhenValidVttFile()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var meeting = new Meeting { Id = meetingId, OrganizerUserId = _userId };
        _contextMock.Setup(x => x.Meetings).ReturnsDbSet(new List<Meeting> { meeting });

        var vttContent = "WEBVTT\n\n00:00.000 --> 00:05.000\n<v Speaker>Hello everyone. We are testing this.";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(vttContent));

        // Act
        await _sut.ProcessTranscriptFileAsync(meetingId, stream, "test.vtt", "text/vtt");

        // Assert
        meeting.TranscriptText.Should().Be("Hello everyone. We are testing this.");
        meeting.TranscriptFileName.Should().Be("test.vtt");
    }

    [Fact]
    public async Task DeleteTranscriptAsync_ShouldClearTranscript_WhenStatusIsReadyForAnalysis()
    {
        // Arrange
        var meetingId = Guid.NewGuid();
        var meeting = new Meeting 
        { 
            Id = meetingId, 
            OrganizerUserId = _userId, 
            TranscriptText = "Some text", 
            Status = MeetingStatus.ReadyForAnalysis 
        };
        _contextMock.Setup(x => x.Meetings).ReturnsDbSet(new List<Meeting> { meeting });

        // Act
        await _sut.DeleteTranscriptAsync(meetingId);

        // Assert
        meeting.TranscriptText.Should().BeNull();
        meeting.Status.Should().Be(MeetingStatus.Draft);
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
