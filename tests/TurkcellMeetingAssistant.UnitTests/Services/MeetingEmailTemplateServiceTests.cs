using FluentAssertions;
using Microsoft.Extensions.Options;
using TurkcellMeetingAssistant.Application.Common.Models;
using TurkcellMeetingAssistant.Application.Services;
using TurkcellMeetingAssistant.Domain.Entities;
using Xunit;

namespace TurkcellMeetingAssistant.UnitTests.Services;

public class MeetingEmailTemplateServiceTests
{
    private readonly MeetingEmailTemplateService _sut;

    public MeetingEmailTemplateServiceTests()
    {
        var settings = Options.Create(new EmailSettings { EmailFooterText = "Footer Text" });
        _sut = new MeetingEmailTemplateService(settings);
    }

    [Fact]
    public async Task GeneratePreviewEmailAsync_ShouldGenerateHtml_WithCorrectDetails()
    {
        // Arrange
        var meeting = new Meeting { Title = "Test Meeting", MeetingDate = new DateTime(2026, 1, 1), OrganizerUser = new User { FirstName = "John", LastName = "Doe" } };
        var summary = new MeetingSummary { ExecutiveSummary = "Summary" };
        var actionItems = new List<ActionItem> { new ActionItem { Description = "Action1", OwnerName = "Owner1", DueDate = new DateTime(2026, 2, 1) } };
        var topics = new List<MeetingTopic> { new MeetingTopic { Title = "Topic1" } };
        var decisions = new List<MeetingDecision> { new MeetingDecision { Description = "Decision1" } };
        var issues = new List<OpenIssue> { new OpenIssue { Description = "Issue1" } };
        var participants = new List<MeetingParticipant> { new MeetingParticipant { FullName = "P1", Email = "p1@test.com" } };

        // Act
        var result = await _sut.GeneratePreviewEmailAsync(meeting, summary, actionItems, topics, decisions, issues, participants, "Subject Override", "Intro", "Closing", true, true, true, true, true, true, false, default);

        // Assert
        result.Subject.Should().Be("Subject Override");
        result.HtmlBody.Should().Contain("Test Meeting");
        result.HtmlBody.Should().Contain("Intro");
        result.HtmlBody.Should().Contain("Closing");
        result.HtmlBody.Should().Contain("John Doe");
        result.HtmlBody.Should().Contain("Action1");
        result.HtmlBody.Should().Contain("Topic1");
        result.HtmlBody.Should().Contain("Decision1");
        result.HtmlBody.Should().Contain("Issue1");
        result.HtmlBody.Should().Contain("p1@test.com");
        result.HtmlBody.Should().Contain("Footer Text");
        
        result.TextBody.Should().Contain("Test Meeting");
    }
}
