using TurkcellMeetingAssistant.Application.Common.Models;
using TurkcellMeetingAssistant.Domain.Entities;

namespace TurkcellMeetingAssistant.Application.Interfaces;

public interface IMeetingEmailTemplateService
{
    Task<EmailMessage> GeneratePreviewEmailAsync(
        Meeting meeting,
        MeetingSummary summary,
        List<ActionItem> actionItems,
        List<MeetingTopic> topics,
        List<MeetingDecision> decisions,
        List<OpenIssue> openIssues,
        List<MeetingParticipant> participants,
        string? subjectOverride = null,
        string? introText = null,
        string? closingText = null,
        bool includeParticipants = true,
        bool includeTopics = true,
        bool includeDecisions = true,
        bool includeActionItems = true,
        bool includeOpenIssues = true,
        bool includeActionStatus = true,
        bool includeEvidence = false,
        CancellationToken cancellationToken = default);
}
