using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using TurkcellMeetingAssistant.Application.Common.Exceptions;
using TurkcellMeetingAssistant.Application.Common.Models;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Email;
using TurkcellMeetingAssistant.Application.Interfaces;
using TurkcellMeetingAssistant.Application.Services;
using TurkcellMeetingAssistant.Domain.Entities;
using TurkcellMeetingAssistant.Domain.Enums;
using TurkcellMeetingAssistant.Infrastructure.Data.Contexts;
using Xunit;

namespace TurkcellMeetingAssistant.UnitTests.Services;

public class MeetingEmailAppServiceTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IMeetingEmailTemplateService> _mockTemplateService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly MeetingEmailAppService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public MeetingEmailAppServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
            
        _dbContext = new ApplicationDbContext(options);
        
        _mockEmailService = new Mock<IEmailService>();
        _mockEmailService.Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailSendResult { IsSuccessful = true, ProviderMessageId = "123", SentAt = DateTime.UtcNow });
            
        _mockTemplateService = new Mock<IMeetingEmailTemplateService>();
        _mockTemplateService.Setup(x => x.GeneratePreviewEmailAsync(
                It.IsAny<Meeting>(), It.IsAny<MeetingSummary>(), It.IsAny<List<ActionItem>>(), 
                It.IsAny<List<MeetingTopic>>(), It.IsAny<List<MeetingDecision>>(), It.IsAny<List<OpenIssue>>(), 
                It.IsAny<List<MeetingParticipant>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailMessage { Subject = "Test Subject", HtmlBody = "<p>Html</p>", TextBody = "Text" });

        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockCurrentUserService.Setup(x => x.UserId).Returns(_userId);
        _mockCurrentUserService.Setup(x => x.Role).Returns(UserRole.User.ToString());

        var settings = Options.Create(new EmailSettings
        {
            AllowExternalRecipients = false,
            AllowedEmailDomains = new[] { "turkcell.com.tr" }
        });

        _sut = new MeetingEmailAppService(_dbContext, _mockEmailService.Object, _mockTemplateService.Object, _mockCurrentUserService.Object, settings);
    }

    [Fact]
    public async Task SendMeetingEmailAsync_ShouldSucceed_WhenValidAndApproved()
    {
        // Arrange
        var user = new User { Id = _userId, Email = "org@test.com", FirstName = "Org", LastName = "User", PasswordHash = "hash" };
        var meeting = new Meeting { Id = Guid.NewGuid(), Title = "M1", OrganizerUserId = _userId, Status = MeetingStatus.Approved };
        var summary = new MeetingSummary { Id = Guid.NewGuid(), MeetingId = meeting.Id, Version = 1, IsApproved = true };
        var participant = new MeetingParticipant { MeetingId = meeting.Id, Email = "test@turkcell.com.tr", FullName = "Test" };
        
        _dbContext.Users.Add(user);
        _dbContext.Meetings.Add(meeting);
        _dbContext.MeetingSummaries.Add(summary);
        _dbContext.MeetingParticipants.Add(participant);
        await _dbContext.SaveChangesAsync();

        var request = new SendMeetingEmailRequest();

        // Act
        var result = await _sut.SendMeetingEmailAsync(meeting.Id, request, default);

        // Assert
        result.Status.Should().Be(EmailDeliveryStatus.Sent);
        meeting.Status.Should().Be(MeetingStatus.EmailSent);
        
        var log = await _dbContext.EmailLogs.FirstOrDefaultAsync();
        log.Should().NotBeNull();
        log!.ToRecipientsJson.Should().Contain("test@turkcell.com.tr");
    }

    [Fact]
    public async Task SendMeetingEmailAsync_ShouldThrowBadRequest_WhenSummaryNotApproved()
    {
        // Arrange
        var user = new User { Id = _userId, Email = "org@test.com", FirstName = "Org", LastName = "User", PasswordHash = "hash" };
        var meeting = new Meeting { Id = Guid.NewGuid(), Title = "M1", OrganizerUserId = _userId, Status = MeetingStatus.WaitingForApproval };
        var summary = new MeetingSummary { Id = Guid.NewGuid(), MeetingId = meeting.Id, Version = 1, IsApproved = false };
        
        _dbContext.Users.Add(user);
        _dbContext.Meetings.Add(meeting);
        _dbContext.MeetingSummaries.Add(summary);
        await _dbContext.SaveChangesAsync();

        var request = new SendMeetingEmailRequest();

        // Act
        Func<Task> act = async () => await _sut.SendMeetingEmailAsync(meeting.Id, request, default);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("*onaylanmamış*");
    }

    [Fact]
    public async Task SendTestEmailAsync_ShouldThrowBadRequest_WhenDomainNotAllowed()
    {
        // Arrange
        var user = new User { Id = _userId, Email = "org@test.com", FirstName = "Org", LastName = "User", PasswordHash = "hash" };
        var meeting = new Meeting { Id = Guid.NewGuid(), Title = "M1", OrganizerUserId = _userId, Status = MeetingStatus.Approved };
        var summary = new MeetingSummary { Id = Guid.NewGuid(), MeetingId = meeting.Id, Version = 1, IsApproved = true };
        
        _dbContext.Users.Add(user);
        _dbContext.Meetings.Add(meeting);
        _dbContext.MeetingSummaries.Add(summary);
        await _dbContext.SaveChangesAsync();

        var request = new SendTestEmailRequest { ToEmail = "test@external.com" };

        // Act
        Func<Task> act = async () => await _sut.SendTestEmailAsync(meeting.Id, request, default);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("*Yasaklı alan adı*");
    }

    [Fact]
    public async Task SendMeetingEmailAsync_ShouldPreventDuplicateIdempotencyKey()
    {
        // Arrange
        var user = new User { Id = _userId, Email = "org@test.com", FirstName = "Org", LastName = "User", PasswordHash = "hash" };
        var meeting = new Meeting { Id = Guid.NewGuid(), Title = "M1", OrganizerUserId = _userId, Status = MeetingStatus.Approved };
        var summary = new MeetingSummary { Id = Guid.NewGuid(), MeetingId = meeting.Id, Version = 1, IsApproved = true };
        var participant = new MeetingParticipant { MeetingId = meeting.Id, Email = "test@turkcell.com.tr", FullName = "Test" };
        
        _dbContext.Users.Add(user);
        _dbContext.Meetings.Add(meeting);
        _dbContext.MeetingSummaries.Add(summary);
        _dbContext.MeetingParticipants.Add(participant);
        
        // Add existing log
        var idempotencyKey = "test-key-123";
        _dbContext.EmailLogs.Add(new EmailLog
        {
            Id = Guid.NewGuid(),
            MeetingId = meeting.Id,
            MeetingSummaryId = summary.Id,
            Status = EmailDeliveryStatus.Sent,
            IdempotencyKey = idempotencyKey,
            EmailType = EmailType.MeetingSummary
        });
        
        await _dbContext.SaveChangesAsync();

        var request = new SendMeetingEmailRequest { IdempotencyKey = idempotencyKey };

        // Act
        var result = await _sut.SendMeetingEmailAsync(meeting.Id, request, default);

        // Assert
        result.Status.Should().Be(EmailDeliveryStatus.Sent);
        // It shouldn't have changed meeting status because it was cached
        meeting.Status.Should().Be(MeetingStatus.Approved);
        
        var logCount = await _dbContext.EmailLogs.CountAsync();
        logCount.Should().Be(1); // No new log created
    }
}
