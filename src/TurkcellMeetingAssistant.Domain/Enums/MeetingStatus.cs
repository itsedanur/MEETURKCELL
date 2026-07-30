namespace TurkcellMeetingAssistant.Domain.Enums;

public enum MeetingStatus
{
    Draft,
    ReadyForAnalysis,
    Analyzing,
    AnalysisCompleted,
    WaitingForApproval,
    Approved,
    EmailSent,
    Archived,
    Failed
}
