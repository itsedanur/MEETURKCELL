using TurkcellMeetingAssistant.Application.DTOs.Meetings.Analysis;

namespace TurkcellMeetingAssistant.Application.Interfaces;

public interface IAiMeetingAnalysisService
{
    Task<MeetingAnalysisResult> AnalyzeAsync(
        MeetingAnalysisInput input,
        CancellationToken cancellationToken);
}
