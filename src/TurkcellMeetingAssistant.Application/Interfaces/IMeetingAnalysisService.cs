using TurkcellMeetingAssistant.Application.DTOs.Meetings.Analysis;

namespace TurkcellMeetingAssistant.Application.Interfaces;

public interface IMeetingAnalysisService
{
    Task<MeetingAnalysisStatusDto> AnalyzeMeetingAsync(Guid meetingId, CancellationToken cancellationToken);
    Task<MeetingAnalysisStatusDto> ReanalyzeMeetingAsync(Guid meetingId, CancellationToken cancellationToken);
    Task<MeetingAnalysisStatusDto> GetAnalysisStatusAsync(Guid meetingId, CancellationToken cancellationToken);
    Task<MeetingSummaryDto> GetMeetingSummaryAsync(Guid meetingId, CancellationToken cancellationToken);
}
