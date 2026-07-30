using TurkcellMeetingAssistant.Application.DTOs.Meetings.Analysis;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Summary;
using TurkcellMeetingAssistant.Application.Common.Models;

namespace TurkcellMeetingAssistant.Application.Interfaces;

public interface IMeetingActionItemService
{
    Task<ActionItemDto> AddActionItemAsync(Guid meetingId, CreateActionItemRequest request, CancellationToken cancellationToken);
    Task<ActionItemDto> UpdateActionItemAsync(Guid meetingId, Guid actionItemId, UpdateActionItemRequest request, CancellationToken cancellationToken);
    Task DeleteActionItemAsync(Guid meetingId, Guid actionItemId, CancellationToken cancellationToken);
    Task<ActionItemDto> UpdateActionItemStatusAsync(Guid meetingId, Guid actionItemId, UpdateActionItemStatusRequest request, CancellationToken cancellationToken);
    
    // Global action item queries
    Task<PaginatedList<ActionItemDto>> GetActionItemsAsync(ActionItemFilterDto filter, CancellationToken cancellationToken);
    Task<PaginatedList<ActionItemDto>> GetMyActionItemsAsync(ActionItemFilterDto filter, CancellationToken cancellationToken);
    Task<ActionItemDto> GetActionItemAsync(Guid actionItemId, CancellationToken cancellationToken);
}
