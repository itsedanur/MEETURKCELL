using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TurkcellMeetingAssistant.Application.DTOs.Speech;
using TurkcellMeetingAssistant.Application.Interfaces.Speech;
using TurkcellMeetingAssistant.Application.Models.Speech;
using TurkcellMeetingAssistant.Domain.Entities;
using TurkcellMeetingAssistant.Domain.Enums;
using TurkcellMeetingAssistant.Infrastructure.Data.Contexts;
using TurkcellMeetingAssistant.Infrastructure.Services.Speech;

namespace TurkcellMeetingAssistant.UnitTests.Services.Speech;

public class MeetingRecordingServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IRecordingStorageService> _storageMock;
    private readonly Mock<ITranscriptionQueue> _queueMock;
    private readonly Mock<ISpeechToTextProvider> _speechProviderMock;
    private readonly Mock<ILogger<MeetingRecordingService>> _loggerMock;
    private readonly IOptions<SpeechToTextSettings> _settings;
    private readonly MeetingRecordingService _service;

    public MeetingRecordingServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);

        _storageMock = new Mock<IRecordingStorageService>();
        _queueMock = new Mock<ITranscriptionQueue>();
        _speechProviderMock = new Mock<ISpeechToTextProvider>();
        _speechProviderMock.Setup(x => x.ProviderName).Returns("Mock");

        _loggerMock = new Mock<ILogger<MeetingRecordingService>>();

        _settings = Options.Create(new SpeechToTextSettings
        {
            AllowedExtensions = new[] { ".mp3" },
            MaxFileSizeMb = 25,
            Provider = "Mock"
        });

        _service = new MeetingRecordingService(
            _dbContext,
            _storageMock.Object,
            _queueMock.Object,
            _speechProviderMock.Object,
            _settings,
            _loggerMock.Object
        );
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task UploadRecordingAsync_ShouldThrowException_WhenMeetingNotFound()
    {
        // Arrange
        var request = new UploadRecordingRequestDto
        {
            MeetingId = Guid.NewGuid(),
            FileName = "test.mp3",
            FileStream = new MemoryStream(),
            FileSize = 1000
        };

        // Act
        var act = () => _service.UploadRecordingAsync(request);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Meeting not found");
    }

    [Fact]
    public async Task UploadRecordingAsync_ShouldThrowException_WhenExtensionNotAllowed()
    {
        // Arrange
        var meeting = new Meeting { Id = Guid.NewGuid(), Title = "Test", Status = MeetingStatus.Draft };
        _dbContext.Meetings.Add(meeting);
        await _dbContext.SaveChangesAsync();

        var request = new UploadRecordingRequestDto
        {
            MeetingId = meeting.Id,
            FileName = "test.exe",
            FileStream = new MemoryStream(),
            FileSize = 1000
        };

        // Act
        var act = () => _service.UploadRecordingAsync(request);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("File format is not supported");
    }

    [Fact]
    public async Task UploadRecordingAsync_ShouldSucceed_AndQueueJob()
    {
        // Arrange
        var meeting = new Meeting { Id = Guid.NewGuid(), Title = "Test", Status = MeetingStatus.Draft };
        _dbContext.Meetings.Add(meeting);
        await _dbContext.SaveChangesAsync();

        var request = new UploadRecordingRequestDto
        {
            MeetingId = meeting.Id,
            FileName = "test.mp3",
            FileStream = new MemoryStream(),
            FileSize = 1000,
            ContentType = "audio/mpeg"
        };

        _storageMock.Setup(s => s.SaveRecordingAsync(It.IsAny<Stream>(), It.IsAny<string>(), default))
            .ReturnsAsync("storage-key-123.mp3");

        // Act
        var result = await _service.UploadRecordingAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(RecordingStatus.Queued);
        result.OriginalFileName.Should().Be("test.mp3");
        
        var recordingInDb = await _dbContext.MeetingRecordings.FirstOrDefaultAsync(r => r.Id == result.Id);
        recordingInDb.Should().NotBeNull();
        recordingInDb!.StorageKey.Should().Be("storage-key-123.mp3");

        _queueMock.Verify(q => q.QueueJobAsync(It.Is<TranscriptionJob>(j => j.MeetingId == meeting.Id && j.RecordingId == result.Id), default), Times.Once);
    }

    [Fact]
    public async Task ProcessRecordingAsync_ShouldUpdateMeetingTranscript_WhenSuccess()
    {
        // Arrange
        var meeting = new Meeting { Id = Guid.NewGuid(), Title = "Test", Status = MeetingStatus.Draft };
        var recording = new MeetingRecording 
        { 
            Id = Guid.NewGuid(), 
            MeetingId = meeting.Id,
            Meeting = meeting,
            Status = RecordingStatus.Queued,
            StorageKey = "test.mp3"
        };
        _dbContext.Meetings.Add(meeting);
        _dbContext.MeetingRecordings.Add(recording);
        await _dbContext.SaveChangesAsync();

        _storageMock.Setup(s => s.GetFullPath(It.IsAny<string>())).Returns("/path/to/test.mp3");
        _speechProviderMock.Setup(s => s.TranscribeAsync(It.IsAny<SpeechToTextRequest>(), default))
            .ReturnsAsync(new SpeechToTextResult { IsSuccess = true, TranscriptText = "Hello AI" });

        // Act
        await _service.ProcessRecordingAsync(recording.Id);

        // Assert
        var updatedRecording = await _dbContext.MeetingRecordings.FindAsync(recording.Id);
        updatedRecording!.Status.Should().Be(RecordingStatus.Completed);

        var updatedMeeting = await _dbContext.Meetings.FindAsync(meeting.Id);
        updatedMeeting!.TranscriptText.Should().Be("Hello AI");
        updatedMeeting.Status.Should().Be(MeetingStatus.ReadyForAnalysis);

        _storageMock.Verify(s => s.DeleteRecordingAsync("test.mp3", default), Times.Once);
    }

    [Fact]
    public async Task ProcessRecordingAsync_ShouldSetFailed_WhenProviderFails()
    {
        // Arrange
        var meeting = new Meeting { Id = Guid.NewGuid(), Title = "Test", Status = MeetingStatus.Draft };
        var recording = new MeetingRecording 
        { 
            Id = Guid.NewGuid(), 
            MeetingId = meeting.Id,
            Meeting = meeting,
            Status = RecordingStatus.Queued,
            StorageKey = "test.mp3"
        };
        _dbContext.Meetings.Add(meeting);
        _dbContext.MeetingRecordings.Add(recording);
        await _dbContext.SaveChangesAsync();

        _storageMock.Setup(s => s.GetFullPath(It.IsAny<string>())).Returns("/path/to/test.mp3");
        _speechProviderMock.Setup(s => s.TranscribeAsync(It.IsAny<SpeechToTextRequest>(), default))
            .ReturnsAsync(new SpeechToTextResult { IsSuccess = false, ErrorMessage = "API Error" });

        // Act
        await _service.ProcessRecordingAsync(recording.Id);

        // Assert
        var updatedRecording = await _dbContext.MeetingRecordings.FindAsync(recording.Id);
        updatedRecording!.Status.Should().Be(RecordingStatus.Failed);
        updatedRecording.SafeErrorMessage.Should().Contain("Transcription failed");

        var updatedMeeting = await _dbContext.Meetings.FindAsync(meeting.Id);
        updatedMeeting!.TranscriptText.Should().BeNull(); // Unchanged
    }
}
