using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TurkcellMeetingAssistant.Application.Common.Models;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Analysis;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Summary;
using TurkcellMeetingAssistant.Application.Interfaces;

namespace TurkcellMeetingAssistant.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/action-items")]
public class ActionItemsController : ControllerBase
{
    private readonly IMeetingActionItemService _actionItemService;

    public ActionItemsController(IMeetingActionItemService actionItemService)
    {
        _actionItemService = actionItemService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedList<ActionItemDto>>> GetActionItems([FromQuery] ActionItemFilterDto filter, CancellationToken cancellationToken)
    {
        var result = await _actionItemService.GetActionItemsAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("my-actions")]
    public async Task<ActionResult<PaginatedList<ActionItemDto>>> GetMyActionItems([FromQuery] ActionItemFilterDto filter, CancellationToken cancellationToken)
    {
        var result = await _actionItemService.GetMyActionItemsAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ActionItemDto>> GetActionItem(Guid id, CancellationToken cancellationToken)
    {
        var result = await _actionItemService.GetActionItemAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ActionItemDto>> UpdateStatus(Guid id, [FromQuery] Guid meetingId, [FromBody] UpdateActionItemStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _actionItemService.UpdateActionItemStatusAsync(meetingId, id, request, cancellationToken);
        return Ok(result);
    }
}
