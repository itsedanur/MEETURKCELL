using TurkcellMeetingAssistant.Application.DTOs.Meetings.Analysis;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Summary;

namespace TurkcellMeetingAssistant.Application.Interfaces;

public interface IMeetingSummaryEditorService
{
    Task<MeetingSummaryDto> UpdateSummaryAsync(Guid meetingId, UpdateMeetingSummaryRequest request, CancellationToken cancellationToken);
    
    Task<MeetingTopicDto> AddTopicAsync(Guid meetingId, CreateMeetingTopicRequest request, CancellationToken cancellationToken);
    Task<MeetingTopicDto> UpdateTopicAsync(Guid meetingId, Guid topicId, UpdateMeetingTopicRequest request, CancellationToken cancellationToken);
    Task DeleteTopicAsync(Guid meetingId, Guid topicId, CancellationToken cancellationToken);
    Task ReorderTopicsAsync(Guid meetingId, ReorderRequest request, CancellationToken cancellationToken);

    Task<MeetingDecisionDto> AddDecisionAsync(Guid meetingId, CreateMeetingDecisionRequest request, CancellationToken cancellationToken);
    Task<MeetingDecisionDto> UpdateDecisionAsync(Guid meetingId, Guid decisionId, UpdateMeetingDecisionRequest request, CancellationToken cancellationToken);
    Task DeleteDecisionAsync(Guid meetingId, Guid decisionId, CancellationToken cancellationToken);
    Task ReorderDecisionsAsync(Guid meetingId, ReorderRequest request, CancellationToken cancellationToken);
    
    Task<OpenIssueDto> AddOpenIssueAsync(Guid meetingId, CreateOpenIssueRequest request, CancellationToken cancellationToken);
    Task<OpenIssueDto> UpdateOpenIssueAsync(Guid meetingId, Guid openIssueId, UpdateOpenIssueRequest request, CancellationToken cancellationToken);
    Task DeleteOpenIssueAsync(Guid meetingId, Guid openIssueId, CancellationToken cancellationToken);
    Task ReorderOpenIssuesAsync(Guid meetingId, ReorderRequest request, CancellationToken cancellationToken);
    
    Task<ApproveMeetingSummaryResponse> ApproveSummaryAsync(Guid meetingId, ApproveMeetingSummaryRequest request, CancellationToken cancellationToken);
    Task RevokeApprovalAsync(Guid meetingId, RevokeApprovalRequest request, CancellationToken cancellationToken);
    
    Task<List<MeetingSummaryVersionDto>> GetSummaryVersionsAsync(Guid meetingId, CancellationToken cancellationToken);
    Task<MeetingSummaryDto> GetSummaryVersionAsync(Guid meetingId, int version, CancellationToken cancellationToken);
}
