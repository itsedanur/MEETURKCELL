using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TurkcellMeetingAssistant.Application.DTOs.Speech;
using TurkcellMeetingAssistant.Application.Interfaces.Speech;

namespace TurkcellMeetingAssistant.Api.Controllers;

[ApiController]
[Route("api/meetings/{meetingId}/[controller]")]
[Authorize]
public class RecordingsController : ControllerBase
{
    private readonly IMeetingRecordingService _recordingService;

    public RecordingsController(IMeetingRecordingService recordingService)
    {
        _recordingService = recordingService;
    }

    [HttpPost]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100 MB allow at IIS/Kestrel level, actual limit checked in service
    public async Task<ActionResult<MeetingRecordingDto>> UploadRecording(
        [FromRoute] Guid meetingId, 
        IFormFile file, 
        [FromForm] string? language, 
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { Message = "File is empty or not provided." });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var request = new UploadRecordingRequestDto
            {
                MeetingId = meetingId,
                FileStream = stream,
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                Language = language
            };

            var result = await _recordingService.UploadRecordingAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Log it appropriately in middleware or here
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<MeetingRecordingDto>>> GetRecordings([FromRoute] Guid meetingId, CancellationToken cancellationToken)
    {
        var results = await _recordingService.GetRecordingsForMeetingAsync(meetingId, cancellationToken);
        return Ok(results);
    }

    [HttpGet("{recordingId}/status")]
    public async Task<ActionResult<MeetingRecordingDto>> GetRecordingStatus([FromRoute] Guid meetingId, [FromRoute] Guid recordingId, CancellationToken cancellationToken)
    {
        var recording = await _recordingService.GetRecordingAsync(meetingId, recordingId, cancellationToken);
        
        if (recording == null)
        {
            return NotFound(new { Message = "Recording not found" });
        }

        return Ok(recording);
    }
}
