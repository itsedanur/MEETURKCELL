using TurkcellMeetingAssistant.Application.Common.Models;
using TurkcellMeetingAssistant.Application.DTOs.Meetings;
using TurkcellMeetingAssistant.Domain.Enums;

namespace TurkcellMeetingAssistant.Application.Interfaces;

public interface IMeetingService
{
    Task<Guid> CreateMeetingAsync(CreateMeetingRequest request, CancellationToken cancellationToken = default);
    
    Task<PagedResult<MeetingListItemDto>> GetMeetingsAsync(
        int pageNumber,
        int pageSize,
        string? search,
        MeetingStatus? status,
        DateTime? meetingDateFrom,
        DateTime? meetingDateTo,
        bool isArchived = false,
        string sortBy = "MeetingDate",
        string sortDirection = "desc",
        CancellationToken cancellationToken = default);

    Task<MeetingDetailDto> GetMeetingByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task UpdateMeetingAsync(Guid id, UpdateMeetingRequest request, CancellationToken cancellationToken = default);
    
    Task ArchiveMeetingAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task RestoreMeetingAsync(Guid id, CancellationToken cancellationToken = default);
}
