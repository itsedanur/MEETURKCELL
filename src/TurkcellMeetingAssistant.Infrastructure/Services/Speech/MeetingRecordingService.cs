using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TurkcellMeetingAssistant.Application.DTOs.Speech;
using TurkcellMeetingAssistant.Application.Interfaces;
using TurkcellMeetingAssistant.Application.Interfaces.Speech;
using TurkcellMeetingAssistant.Application.Models.Speech;
using TurkcellMeetingAssistant.Domain.Entities;
using TurkcellMeetingAssistant.Domain.Enums;

namespace TurkcellMeetingAssistant.Infrastructure.Services.Speech;

public class MeetingRecordingService : IMeetingRecordingService
{
    private readonly IApplicationDbContext _context;
    private readonly IRecordingStorageService _storageService;
    private readonly ITranscriptionQueue _queue;
    private readonly ISpeechToTextProvider _speechProvider;
    private readonly SpeechToTextSettings _settings;
    private readonly ILogger<MeetingRecordingService> _logger;

    public MeetingRecordingService(
        IApplicationDbContext context,
        IRecordingStorageService storageService,
        ITranscriptionQueue queue,
        ISpeechToTextProvider speechProvider,
        IOptions<SpeechToTextSettings> settings,
        ILogger<MeetingRecordingService> logger)
    {
        _context = context;
        _storageService = storageService;
        _queue = queue;
        _speechProvider = speechProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<MeetingRecordingDto> UploadRecordingAsync(UploadRecordingRequestDto request, CancellationToken cancellationToken = default)
    {
        var meeting = await _context.Meetings.FirstOrDefaultAsync(m => m.Id == request.MeetingId, cancellationToken);
        if (meeting == null)
            throw new Exception("Meeting not found");

        if (meeting.Status == MeetingStatus.Archived || meeting.Status == MeetingStatus.EmailSent)
            throw new Exception("Cannot upload recording to Archived or EmailSent meetings");

        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        if (!_settings.AllowedExtensions.Contains(extension))
            throw new Exception("File format is not supported");

        if (request.FileSize > _settings.MaxFileSizeMb * 1024 * 1024)
            throw new Exception($"File size exceeds {_settings.MaxFileSizeMb}MB limit");

        var storageKey = await _storageService.SaveRecordingAsync(request.FileStream, request.FileName, cancellationToken);

        var recording = new MeetingRecording
        {
            Id = Guid.NewGuid(),
            MeetingId = request.MeetingId,
            OriginalFileName = request.FileName,
            StorageKey = storageKey,
            ContentType = request.ContentType,
            FileSize = request.FileSize,
            Status = RecordingStatus.Queued,
            SpeechProvider = _speechProvider.ProviderName,
            Language = request.Language ?? _settings.DefaultLanguage,
            CreatedAt = DateTime.UtcNow
        };

        _context.MeetingRecordings.Add(recording);
        await _context.SaveChangesAsync(cancellationToken);

        // Queue the job
        await _queue.QueueJobAsync(new TranscriptionJob
        {
            MeetingId = recording.MeetingId,
            RecordingId = recording.Id
        }, cancellationToken);

        return MapToDto(recording);
    }

    public async Task<List<MeetingRecordingDto>> GetRecordingsForMeetingAsync(Guid meetingId, CancellationToken cancellationToken = default)
    {
        var recordings = await _context.MeetingRecordings
            .Where(r => r.MeetingId == meetingId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return recordings.Select(MapToDto).ToList();
    }

    public async Task<MeetingRecordingDto?> GetRecordingAsync(Guid meetingId, Guid recordingId, CancellationToken cancellationToken = default)
    {
        var recording = await _context.MeetingRecordings
            .FirstOrDefaultAsync(r => r.MeetingId == meetingId && r.Id == recordingId, cancellationToken);

        return recording != null ? MapToDto(recording) : null;
    }

    public async Task ProcessRecordingAsync(Guid recordingId, CancellationToken cancellationToken = default)
    {
        var recording = await _context.MeetingRecordings
            .Include(r => r.Meeting)
            .FirstOrDefaultAsync(r => r.Id == recordingId, cancellationToken);

        if (recording == null) return;

        try
        {
            recording.Status = RecordingStatus.Processing;
            recording.ProcessingStartedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            var filePath = _storageService.GetFullPath(recording.StorageKey);

            var result = await _speechProvider.TranscribeAsync(new SpeechToTextRequest
            {
                RecordingId = recordingId,
                FilePath = filePath,
                Language = recording.Language ?? "tr"
            }, cancellationToken);

            if (result.IsSuccess)
            {
                recording.Status = RecordingStatus.Completed;
                recording.ProcessingCompletedAt = DateTime.UtcNow;
                recording.DetectedLanguage = result.DetectedLanguage;

                // Update meeting transcript
                recording.Meeting.TranscriptText = result.TranscriptText;
                recording.Meeting.TranscriptFileName = recording.OriginalFileName;
                recording.Meeting.TranscriptFileType = recording.ContentType;
                recording.Meeting.TranscriptUploadedAt = DateTime.UtcNow;
                
                if (recording.Meeting.Status == MeetingStatus.Draft)
                {
                    recording.Meeting.Status = MeetingStatus.ReadyForAnalysis;
                }
            }
            else
            {
                recording.Status = RecordingStatus.Failed;
                recording.ProcessingCompletedAt = DateTime.UtcNow;
                recording.SafeErrorMessage = "Transcription failed. Please try again or use another file.";
                _logger.LogError("Transcription failed for recording {RecordingId}. Error: {Error}", recordingId, result.ErrorMessage);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing recording {RecordingId}", recordingId);
            await UpdateStatusAsync(recordingId, RecordingStatus.Failed, "SysError", "An unexpected error occurred processing your file.", cancellationToken);
        }
        finally
        {
            // Delete file safely
            try
            {
                await _storageService.DeleteRecordingAsync(recording.StorageKey, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up file {StorageKey}", recording.StorageKey);
            }
        }
    }

    public async Task UpdateStatusAsync(Guid recordingId, RecordingStatus status, string? errorCode = null, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        var recording = await _context.MeetingRecordings.FindAsync(new object[] { recordingId }, cancellationToken);
        if (recording != null)
        {
            recording.Status = status;
            recording.ErrorCode = errorCode;
            recording.SafeErrorMessage = errorMessage;
            if (status == RecordingStatus.Failed || status == RecordingStatus.Completed)
            {
                recording.ProcessingCompletedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private static MeetingRecordingDto MapToDto(MeetingRecording entity)
    {
        return new MeetingRecordingDto
        {
            Id = entity.Id,
            MeetingId = entity.MeetingId,
            OriginalFileName = entity.OriginalFileName,
            ContentType = entity.ContentType,
            FileSize = entity.FileSize,
            Status = entity.Status,
            ErrorCode = entity.ErrorCode,
            SafeErrorMessage = entity.SafeErrorMessage,
            CreatedAt = entity.CreatedAt,
            ProcessingStartedAt = entity.ProcessingStartedAt,
            ProcessingCompletedAt = entity.ProcessingCompletedAt
        };
    }
}
