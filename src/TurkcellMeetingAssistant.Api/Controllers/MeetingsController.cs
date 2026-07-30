using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TurkcellMeetingAssistant.Application.Common.Models;
using TurkcellMeetingAssistant.Application.DTOs.Meetings;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Analysis;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Summary;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Email;
using TurkcellMeetingAssistant.Application.Interfaces;
using TurkcellMeetingAssistant.Application.Services;
using TurkcellMeetingAssistant.Domain.Enums;

namespace TurkcellMeetingAssistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MeetingsController : ControllerBase
{
    private readonly IMeetingService _meetingService;
    private readonly IMeetingParticipantService _participantService;
    private readonly ITranscriptService _transcriptService;
    private readonly IMeetingAnalysisService _analysisService;
    private readonly IMeetingSummaryEditorService _summaryEditorService;
    private readonly IMeetingActionItemService _actionItemService;

    public MeetingsController(
        IMeetingService meetingService,
        IMeetingParticipantService participantService,
        ITranscriptService transcriptService,
        IMeetingAnalysisService analysisService,
        IMeetingSummaryEditorService summaryEditorService,
        IMeetingActionItemService actionItemService)
    {
        _meetingService = meetingService;
        _participantService = participantService;
        _transcriptService = transcriptService;
        _analysisService = analysisService;
        _summaryEditorService = summaryEditorService;
        _actionItemService = actionItemService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateMeeting(
        [FromBody] CreateMeetingRequest request, 
        CancellationToken cancellationToken)
    {
        var id = await _meetingService.CreateMeetingAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetMeeting), new { id }, ApiResponse<Guid>.Success(id));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<MeetingListItemDto>>>> GetMeetings(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] MeetingStatus? status = null,
        [FromQuery] DateTime? meetingDateFrom = null,
        [FromQuery] DateTime? meetingDateTo = null,
        [FromQuery] bool isArchived = false,
        [FromQuery] string sortBy = "MeetingDate",
        [FromQuery] string sortDirection = "desc",
        CancellationToken cancellationToken = default)
    {
        var result = await _meetingService.GetMeetingsAsync(pageNumber, pageSize, search, status, meetingDateFrom, meetingDateTo, isArchived, sortBy, sortDirection, cancellationToken);
        return Ok(ApiResponse<PagedResult<MeetingListItemDto>>.Success(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<MeetingDetailDto>>> GetMeeting(Guid id, CancellationToken cancellationToken)
    {
        var result = await _meetingService.GetMeetingByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<MeetingDetailDto>.Success(result));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateMeeting(
        Guid id, 
        [FromBody] UpdateMeetingRequest request, 
        CancellationToken cancellationToken)
    {
        await _meetingService.UpdateMeetingAsync(id, request, cancellationToken);
        return Ok(ApiResponse<object>.Success(null));
    }

    [HttpPost("{id}/archive")]
    public async Task<ActionResult<ApiResponse<object>>> ArchiveMeeting(Guid id, CancellationToken cancellationToken)
    {
        await _meetingService.ArchiveMeetingAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Success(null));
    }

    [HttpPost("{id}/restore")]
    public async Task<ActionResult<ApiResponse<object>>> RestoreMeeting(Guid id, CancellationToken cancellationToken)
    {
        await _meetingService.RestoreMeetingAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Success(null));
    }

    [HttpPost("{id}/participants")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddParticipant(
        Guid id, 
        [FromBody] CreateParticipantRequest request, 
        CancellationToken cancellationToken)
    {
        var participantId = await _participantService.AddParticipantAsync(id, request, cancellationToken);
        return StatusCode(201, ApiResponse<Guid>.Success(participantId));
    }

    [HttpPut("{meetingId}/participants/{participantId}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateParticipant(
        Guid meetingId, 
        Guid participantId, 
        [FromBody] UpdateParticipantRequest request, 
        CancellationToken cancellationToken)
    {
        await _participantService.UpdateParticipantAsync(meetingId, participantId, request, cancellationToken);
        return Ok(ApiResponse<object>.Success(null));
    }

    [HttpDelete("{meetingId}/participants/{participantId}")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveParticipant(
        Guid meetingId, 
        Guid participantId, 
        CancellationToken cancellationToken)
    {
        await _participantService.RemoveParticipantAsync(meetingId, participantId, cancellationToken);
        return Ok(ApiResponse<object>.Success(null));
    }

    [HttpPut("{id}/transcript/text")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateTranscriptText(
        Guid id, 
        [FromBody] TranscriptTextUpdateRequest request, 
        CancellationToken cancellationToken)
    {
        await _transcriptService.UpdateTranscriptTextAsync(id, request, cancellationToken);
        return Ok(ApiResponse<object>.Success(null));
    }

    [HttpPost("{id}/transcript/file")]
    public async Task<ActionResult<ApiResponse<object>>> UploadTranscriptFile(
        Guid id, 
        IFormFile file, 
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("Dosya seçilmedi veya boş."));

        using var stream = file.OpenReadStream();
        await _transcriptService.ProcessTranscriptFileAsync(id, stream, file.FileName, file.ContentType, cancellationToken);
        return Ok(ApiResponse<object>.Success(null));
    }

    [HttpGet("{id}/transcript")]
    public async Task<ActionResult<ApiResponse<TranscriptResponseDto>>> GetTranscript(Guid id, CancellationToken cancellationToken)
    {
        var result = await _transcriptService.GetTranscriptAsync(id, cancellationToken);
        return Ok(ApiResponse<TranscriptResponseDto>.Success(result));
    }

    [HttpDelete("{id}/transcript")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteTranscript(Guid id, CancellationToken cancellationToken)
    {
        await _transcriptService.DeleteTranscriptAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Success(null));
    }

    [HttpPost("{id}/analyze")]
    public async Task<ActionResult<ApiResponse<MeetingAnalysisStatusDto>>> AnalyzeMeeting(Guid id, CancellationToken cancellationToken)
    {
        var result = await _analysisService.AnalyzeMeetingAsync(id, cancellationToken);
        return Ok(ApiResponse<MeetingAnalysisStatusDto>.Success(result));
    }

    [HttpPost("{id}/reanalyze")]
    public async Task<ActionResult<ApiResponse<MeetingAnalysisStatusDto>>> ReanalyzeMeeting(Guid id, CancellationToken cancellationToken)
    {
        var result = await _analysisService.ReanalyzeMeetingAsync(id, cancellationToken);
        return Ok(ApiResponse<MeetingAnalysisStatusDto>.Success(result));
    }

    [HttpGet("{id}/analysis-status")]
    public async Task<ActionResult<ApiResponse<MeetingAnalysisStatusDto>>> GetAnalysisStatus(Guid id, CancellationToken cancellationToken)
    {
        var result = await _analysisService.GetAnalysisStatusAsync(id, cancellationToken);
        return Ok(ApiResponse<MeetingAnalysisStatusDto>.Success(result));
    }

    [HttpGet("{id}/summary")]
    public async Task<ActionResult<ApiResponse<MeetingSummaryDto>>> GetMeetingSummary(Guid id, CancellationToken cancellationToken)
    {
        var result = await _analysisService.GetMeetingSummaryAsync(id, cancellationToken);
        return Ok(ApiResponse<MeetingSummaryDto>.Success(result));
    }

    // --- Phase 5: Editor Endpoints ---

    [HttpPut("{id}/summary")]
    public async Task<ActionResult<MeetingSummaryDto>> UpdateSummary(Guid id, [FromBody] UpdateMeetingSummaryRequest request, CancellationToken cancellationToken)
    {
        var result = await _summaryEditorService.UpdateSummaryAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id}/summary/topics")]
    public async Task<ActionResult<MeetingTopicDto>> AddTopic(Guid id, [FromBody] CreateMeetingTopicRequest request, CancellationToken cancellationToken)
    {
        var result = await _summaryEditorService.AddTopicAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}/summary/topics/{topicId}")]
    public async Task<ActionResult<MeetingTopicDto>> UpdateTopic(Guid id, Guid topicId, [FromBody] UpdateMeetingTopicRequest request, CancellationToken cancellationToken)
    {
        var result = await _summaryEditorService.UpdateTopicAsync(id, topicId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}/summary/topics/{topicId}")]
    public async Task<ActionResult> DeleteTopic(Guid id, Guid topicId, CancellationToken cancellationToken)
    {
        await _summaryEditorService.DeleteTopicAsync(id, topicId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/summary/topics/reorder")]
    public async Task<ActionResult> ReorderTopics(Guid id, [FromBody] ReorderRequest request, CancellationToken cancellationToken)
    {
        await _summaryEditorService.ReorderTopicsAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/summary/decisions")]
    public async Task<ActionResult<MeetingDecisionDto>> AddDecision(Guid id, [FromBody] CreateMeetingDecisionRequest request, CancellationToken cancellationToken)
    {
        var result = await _summaryEditorService.AddDecisionAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}/summary/decisions/{decisionId}")]
    public async Task<ActionResult<MeetingDecisionDto>> UpdateDecision(Guid id, Guid decisionId, [FromBody] UpdateMeetingDecisionRequest request, CancellationToken cancellationToken)
    {
        var result = await _summaryEditorService.UpdateDecisionAsync(id, decisionId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}/summary/decisions/{decisionId}")]
    public async Task<ActionResult> DeleteDecision(Guid id, Guid decisionId, CancellationToken cancellationToken)
    {
        await _summaryEditorService.DeleteDecisionAsync(id, decisionId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/summary/decisions/reorder")]
    public async Task<ActionResult> ReorderDecisions(Guid id, [FromBody] ReorderRequest request, CancellationToken cancellationToken)
    {
        await _summaryEditorService.ReorderDecisionsAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/summary/action-items")]
    public async Task<ActionResult<ActionItemDto>> AddActionItem(Guid id, [FromBody] CreateActionItemRequest request, CancellationToken cancellationToken)
    {
        var result = await _actionItemService.AddActionItemAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}/summary/action-items/{actionItemId}")]
    public async Task<ActionResult<ActionItemDto>> UpdateActionItem(Guid id, Guid actionItemId, [FromBody] UpdateActionItemRequest request, CancellationToken cancellationToken)
    {
        var result = await _actionItemService.UpdateActionItemAsync(id, actionItemId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}/summary/action-items/{actionItemId}")]
    public async Task<ActionResult> DeleteActionItem(Guid id, Guid actionItemId, CancellationToken cancellationToken)
    {
        await _actionItemService.DeleteActionItemAsync(id, actionItemId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/summary/open-issues")]
    public async Task<ActionResult<OpenIssueDto>> AddOpenIssue(Guid id, [FromBody] CreateOpenIssueRequest request, CancellationToken cancellationToken)
    {
        var result = await _summaryEditorService.AddOpenIssueAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}/summary/open-issues/{openIssueId}")]
    public async Task<ActionResult<OpenIssueDto>> UpdateOpenIssue(Guid id, Guid openIssueId, [FromBody] UpdateOpenIssueRequest request, CancellationToken cancellationToken)
    {
        var result = await _summaryEditorService.UpdateOpenIssueAsync(id, openIssueId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}/summary/open-issues/{openIssueId}")]
    public async Task<ActionResult> DeleteOpenIssue(Guid id, Guid openIssueId, CancellationToken cancellationToken)
    {
        await _summaryEditorService.DeleteOpenIssueAsync(id, openIssueId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/summary/open-issues/reorder")]
    public async Task<ActionResult> ReorderOpenIssues(Guid id, [FromBody] ReorderRequest request, CancellationToken cancellationToken)
    {
        await _summaryEditorService.ReorderOpenIssuesAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/approve")]
    public async Task<ActionResult<ApproveMeetingSummaryResponse>> ApproveSummary(Guid id, [FromBody] ApproveMeetingSummaryRequest request, CancellationToken cancellationToken)
    {
        var result = await _summaryEditorService.ApproveSummaryAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id}/revoke-approval")]
    public async Task<ActionResult> RevokeApproval(Guid id, [FromBody] RevokeApprovalRequest request, CancellationToken cancellationToken)
    {
        await _summaryEditorService.RevokeApprovalAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id}/summary/versions")]
    public async Task<ActionResult<List<MeetingSummaryVersionDto>>> GetSummaryVersions(Guid id, CancellationToken cancellationToken)
    {
        var result = await _summaryEditorService.GetSummaryVersionsAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}/summary/versions/{version}")]
    public async Task<ActionResult<MeetingSummaryDto>> GetSummaryVersion(Guid id, int version, CancellationToken cancellationToken)
    {
        var result = await _summaryEditorService.GetSummaryVersionAsync(id, version, cancellationToken);
        return Ok(result);
    }

    #region E-posta Ön İzleme ve Gönderim (Faz 6)

    [HttpPost("{id}/email/preview")]
    public async Task<ActionResult<EmailPreviewDto>> PreviewEmail(Guid id, [FromBody] TurkcellMeetingAssistant.Application.DTOs.Meetings.Email.GenerateEmailPreviewRequest request)
    {
        var appService = HttpContext.RequestServices.GetRequiredService<IMeetingEmailAppService>();
        var result = await appService.PreviewEmailAsync(id, request, HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpPost("{id}/email/send")]
    public async Task<ActionResult<TurkcellMeetingAssistant.Application.DTOs.Meetings.Email.EmailLogDto>> SendMeetingEmail(Guid id, [FromBody] TurkcellMeetingAssistant.Application.DTOs.Meetings.Email.SendMeetingEmailRequest request)
    {
        var appService = HttpContext.RequestServices.GetRequiredService<IMeetingEmailAppService>();
        var result = await appService.SendMeetingEmailAsync(id, request, HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpPost("{id}/email/test")]
    public async Task<ActionResult<TurkcellMeetingAssistant.Application.DTOs.Meetings.Email.EmailLogDto>> SendTestEmail(Guid id, [FromBody] TurkcellMeetingAssistant.Application.DTOs.Meetings.Email.SendTestEmailRequest request)
    {
        var appService = HttpContext.RequestServices.GetRequiredService<IMeetingEmailAppService>();
        var result = await appService.SendTestEmailAsync(id, request, HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpGet("{id}/email/logs")]
    public async Task<ActionResult<List<TurkcellMeetingAssistant.Application.DTOs.Meetings.Email.EmailLogDto>>> GetEmailLogs(Guid id)
    {
        var appService = HttpContext.RequestServices.GetRequiredService<IMeetingEmailAppService>();
        var result = await appService.GetEmailLogsAsync(id, HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpGet("{id}/email/logs/{logId}")]
    public async Task<ActionResult<TurkcellMeetingAssistant.Application.DTOs.Meetings.Email.EmailLogDetailDto>> GetEmailLogDetail(Guid id, Guid logId)
    {
        var appService = HttpContext.RequestServices.GetRequiredService<IMeetingEmailAppService>();
        var result = await appService.GetEmailLogDetailAsync(id, logId, HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpGet("{id}/email/status")]
    public async Task<ActionResult<TurkcellMeetingAssistant.Application.DTOs.Meetings.Email.EmailStatusDto>> GetEmailStatus(Guid id)
    {
        var appService = HttpContext.RequestServices.GetRequiredService<IMeetingEmailAppService>();
        var result = await appService.GetEmailStatusAsync(id, HttpContext.RequestAborted);
        return Ok(result);
    }

    #endregion
}
