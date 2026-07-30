using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using TurkcellMeetingAssistant.Application.Common.Exceptions;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Analysis;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Email;
using TurkcellMeetingAssistant.Application.Interfaces;
using TurkcellMeetingAssistant.Application.Services;
using TurkcellMeetingAssistant.Domain.Entities;
using TurkcellMeetingAssistant.Domain.Enums;
using TurkcellMeetingAssistant.Application.Common.Models;
using TurkcellMeetingAssistant.Infrastructure.Data.Contexts;
using Xunit;

namespace TurkcellMeetingAssistant.UnitTests.Services;

public class Phase6ScenariosTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IMeetingEmailTemplateService> _mockTemplateService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IMeetingSummaryMutationService> _mockMutationService;
    private readonly EmailSettings _emailSettings;
    private readonly MeetingEmailAppService _sut;
    private readonly MeetingActionItemService _actionService;
    private readonly Guid _userId = Guid.NewGuid();

    public Phase6ScenariosTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _dbContext = new ApplicationDbContext(options);

        _mockEmailService = new Mock<IEmailService>();
        _mockTemplateService = new Mock<IMeetingEmailTemplateService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockMutationService = new Mock<IMeetingSummaryMutationService>();

        _mockCurrentUserService.Setup(c => c.UserId).Returns(_userId);
        _mockCurrentUserService.Setup(c => c.Role).Returns(UserRole.User.ToString());

        _emailSettings = new EmailSettings
        {
            Provider = "Mock",
            DefaultSenderEmail = "no-reply@test.com",
            DefaultSenderName = "Test",
            StoreEmailBody = true,
            MaximumRecipientCount = 10,
            AllowExternalRecipients = false,
            AllowedEmailDomains = new string[] { "turkcell.com.tr", "test.com" },
            EnableTestEmail = true
        };

        var optionsMock = new Mock<IOptions<EmailSettings>>();
        optionsMock.Setup(o => o.Value).Returns(_emailSettings);

        _sut = new MeetingEmailAppService(
            _dbContext,
            _mockEmailService.Object,
            _mockTemplateService.Object,
            _mockCurrentUserService.Object,
            optionsMock.Object
        );
        
        _actionService = new MeetingActionItemService(_dbContext, _mockMutationService.Object, _mockCurrentUserService.Object);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task ApprovalHashBozukken_Preview_Engellenmeli()
    {
        var user = new User { Id = _userId, Email = "test@test.com", FirstName = "T", LastName = "T" };
        var meeting = new Meeting { Id = Guid.NewGuid(), Title = "M", OrganizerUserId = _userId, Status = MeetingStatus.WaitingForApproval };
        var summary = new MeetingSummary { Id = Guid.NewGuid(), MeetingId = meeting.Id, Version = 1, IsApproved = false };
        _dbContext.Users.Add(user);
        _dbContext.Meetings.Add(meeting);
        _dbContext.MeetingSummaries.Add(summary);
        await _dbContext.SaveChangesAsync();

        await _sut.Invoking(s => s.PreviewEmailAsync(meeting.Id, new GenerateEmailPreviewRequest(), CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Özet henüz onaylanmamış veya onayı düşmüş.*");
    }

    [Fact]
    public async Task DisDomain_Engeli_Calismali()
    {
        var user = new User { Id = _userId, Email = "test@test.com", FirstName = "T", LastName = "T" };
        var meeting = new Meeting { Id = Guid.NewGuid(), Title = "M", OrganizerUserId = _userId, Status = MeetingStatus.Approved };
        var summary = new MeetingSummary { Id = Guid.NewGuid(), MeetingId = meeting.Id, Version = 1, IsApproved = true };
        _dbContext.Users.Add(user);
        _dbContext.Meetings.Add(meeting);
        _dbContext.MeetingSummaries.Add(summary);
        await _dbContext.SaveChangesAsync();

        _mockTemplateService.Setup(t => t.GeneratePreviewEmailAsync(It.IsAny<Meeting>(), It.IsAny<MeetingSummary>(), It.IsAny<List<ActionItem>>(), It.IsAny<List<MeetingTopic>>(), It.IsAny<List<MeetingDecision>>(), It.IsAny<List<OpenIssue>>(), It.IsAny<List<MeetingParticipant>>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailMessage { Subject = "s", HtmlBody = "h", TextBody = "t" });

        var request = new SendMeetingEmailRequest
        {
            AdditionalTo = new List<EmailRecipientRequest> { new EmailRecipientRequest { Email = "hacker@evil.com" } }
        };

        await _sut.Invoking(s => s.SendMeetingEmailAsync(meeting.Id, request, CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Yasaklı alan adı: evil.com*");
    }

    [Fact]
    public async Task RecipientLimiti_Asildiginda_Engellenmeli()
    {
        var user = new User { Id = _userId, Email = "test@test.com", FirstName = "T", LastName = "T" };
        var meeting = new Meeting { Id = Guid.NewGuid(), Title = "M", OrganizerUserId = _userId, Status = MeetingStatus.Approved };
        var summary = new MeetingSummary { Id = Guid.NewGuid(), MeetingId = meeting.Id, Version = 1, IsApproved = true };
        _dbContext.Users.Add(user);
        _dbContext.Meetings.Add(meeting);
        _dbContext.MeetingSummaries.Add(summary);
        await _dbContext.SaveChangesAsync();

        _mockTemplateService.Setup(t => t.GeneratePreviewEmailAsync(It.IsAny<Meeting>(), It.IsAny<MeetingSummary>(), It.IsAny<List<ActionItem>>(), It.IsAny<List<MeetingTopic>>(), It.IsAny<List<MeetingDecision>>(), It.IsAny<List<OpenIssue>>(), It.IsAny<List<MeetingParticipant>>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailMessage { Subject = "s", HtmlBody = "h", TextBody = "t" });

        var req = new SendMeetingEmailRequest { AdditionalTo = new List<EmailRecipientRequest>() };
        for (int i = 0; i < 15; i++) req.AdditionalTo.Add(new EmailRecipientRequest { Email = $"user{i}@test.com" });

        await _sut.Invoking(s => s.SendMeetingEmailAsync(meeting.Id, req, CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Maximum alıcı sayısını*");
    }

    [Fact]
    public async Task PendingIdempotencyKey_Davranisi()
    {
        var user = new User { Id = _userId, Email = "test@test.com", FirstName = "T", LastName = "T" };
        var meeting = new Meeting { Id = Guid.NewGuid(), Title = "M", OrganizerUserId = _userId, Status = MeetingStatus.Approved };
        var summary = new MeetingSummary { Id = Guid.NewGuid(), MeetingId = meeting.Id, Version = 1, IsApproved = true };
        
        var emailLog = new EmailLog { Id = Guid.NewGuid(), MeetingId = meeting.Id, Status = EmailDeliveryStatus.Pending, IdempotencyKey = "key1", EmailType = EmailType.MeetingSummary, CreatedAt = DateTime.UtcNow };
        
        _dbContext.Users.Add(user);
        _dbContext.Meetings.Add(meeting);
        _dbContext.MeetingSummaries.Add(summary);
        _dbContext.EmailLogs.Add(emailLog);
        await _dbContext.SaveChangesAsync();

        await _sut.Invoking(s => s.SendMeetingEmailAsync(meeting.Id, new SendMeetingEmailRequest { IdempotencyKey = "key1" }, CancellationToken.None))
            .Should().ThrowAsync<ConcurrencyException>()
            .WithMessage("*Bu e-posta zaten gönderiliyor.*");
    }

    [Fact]
    public async Task FailedKey_Yeniden_Kullanilamamali()
    {
        var user = new User { Id = _userId, Email = "test@test.com", FirstName = "T", LastName = "T" };
        var meeting = new Meeting { Id = Guid.NewGuid(), Title = "M", OrganizerUserId = _userId, Status = MeetingStatus.Approved };
        var summary = new MeetingSummary { Id = Guid.NewGuid(), MeetingId = meeting.Id, Version = 1, IsApproved = true };
        
        var emailLog = new EmailLog { Id = Guid.NewGuid(), MeetingId = meeting.Id, Status = EmailDeliveryStatus.Failed, IdempotencyKey = "key2", EmailType = EmailType.MeetingSummary, CreatedAt = DateTime.UtcNow };
        
        _dbContext.Users.Add(user);
        _dbContext.Meetings.Add(meeting);
        _dbContext.MeetingSummaries.Add(summary);
        _dbContext.EmailLogs.Add(emailLog);
        await _dbContext.SaveChangesAsync();

        var res = await _sut.SendMeetingEmailAsync(meeting.Id, new SendMeetingEmailRequest { IdempotencyKey = "key2" }, CancellationToken.None);
        res.Status.Should().Be(EmailDeliveryStatus.Failed);
    }

    [Fact]
    public async Task MockProvider_Basarisizliginda_MeetingStatus_Approved_Kalmali()
    {
        var user = new User { Id = _userId, Email = "test@test.com", FirstName = "T", LastName = "T" };
        var meeting = new Meeting { Id = Guid.NewGuid(), Title = "M", OrganizerUserId = _userId, Status = MeetingStatus.Approved };
        var summary = new MeetingSummary { Id = Guid.NewGuid(), MeetingId = meeting.Id, Version = 1, IsApproved = true };
        _dbContext.Users.Add(user);
        _dbContext.Meetings.Add(meeting);
        _dbContext.MeetingSummaries.Add(summary);
        await _dbContext.SaveChangesAsync();

        _mockTemplateService.Setup(t => t.GeneratePreviewEmailAsync(It.IsAny<Meeting>(), It.IsAny<MeetingSummary>(), It.IsAny<List<ActionItem>>(), It.IsAny<List<MeetingTopic>>(), It.IsAny<List<MeetingDecision>>(), It.IsAny<List<OpenIssue>>(), It.IsAny<List<MeetingParticipant>>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailMessage { Subject = "s", HtmlBody = "h", TextBody = "t" });

        _mockEmailService.Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailSendResult { IsSuccessful = false, ErrorMessage = "Error" });

        await _sut.SendMeetingEmailAsync(meeting.Id, new SendMeetingEmailRequest(), CancellationToken.None);

        var dbMeeting = await _dbContext.Meetings.FindAsync(meeting.Id);
        dbMeeting!.Status.Should().Be(MeetingStatus.Approved);
    }

    [Fact]
    public async Task TestEpostasi_MeetingStatusu_Degistirmemeli()
    {
        var user = new User { Id = _userId, Email = "test@test.com", FirstName = "T", LastName = "T" };
        var meeting = new Meeting { Id = Guid.NewGuid(), Title = "M", OrganizerUserId = _userId, Status = MeetingStatus.Approved };
        var summary = new MeetingSummary { Id = Guid.NewGuid(), MeetingId = meeting.Id, Version = 1, IsApproved = true };
        _dbContext.Users.Add(user);
        _dbContext.Meetings.Add(meeting);
        _dbContext.MeetingSummaries.Add(summary);
        await _dbContext.SaveChangesAsync();

        _mockTemplateService.Setup(t => t.GeneratePreviewEmailAsync(It.IsAny<Meeting>(), It.IsAny<MeetingSummary>(), It.IsAny<List<ActionItem>>(), It.IsAny<List<MeetingTopic>>(), It.IsAny<List<MeetingDecision>>(), It.IsAny<List<OpenIssue>>(), It.IsAny<List<MeetingParticipant>>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailMessage { Subject = "s", HtmlBody = "h", TextBody = "t" });

        _mockEmailService.Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailSendResult { IsSuccessful = true });

        await _sut.SendTestEmailAsync(meeting.Id, new SendTestEmailRequest { ToEmail = "test@test.com" }, CancellationToken.None);

        var dbMeeting = await _dbContext.Meetings.FindAsync(meeting.Id);
        dbMeeting!.Status.Should().Be(MeetingStatus.Approved);
    }

    [Fact]
    public async Task EmailLogDetayinda_Farkli_MeetingId_Engellenmeli()
    {
        var user = new User { Id = _userId, Email = "test@test.com", FirstName = "T", LastName = "T" };
        var meeting1 = new Meeting { Id = Guid.NewGuid(), Title = "M1", OrganizerUserId = _userId, Status = MeetingStatus.Approved };
        var meeting2 = new Meeting { Id = Guid.NewGuid(), Title = "M2", OrganizerUserId = _userId, Status = MeetingStatus.Approved };
        
        var logForM2 = new EmailLog { Id = Guid.NewGuid(), MeetingId = meeting2.Id, CreatedAt = DateTime.UtcNow };
        
        _dbContext.Users.Add(user);
        _dbContext.Meetings.AddRange(meeting1, meeting2);
        _dbContext.EmailLogs.Add(logForM2);
        await _dbContext.SaveChangesAsync();

        await _sut.Invoking(s => s.GetEmailLogDetailAsync(meeting1.Id, logForM2.Id, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task EmailStatusEndpointi_CanSend_Hesaplamasi()
    {
        var user = new User { Id = _userId, Email = "test@test.com", FirstName = "T", LastName = "T" };
        var meeting = new Meeting { Id = Guid.NewGuid(), Title = "M", OrganizerUserId = _userId, Status = MeetingStatus.Approved };
        var summary = new MeetingSummary { Id = Guid.NewGuid(), MeetingId = meeting.Id, Version = 1, IsApproved = true };
        _dbContext.Users.Add(user);
        _dbContext.Meetings.Add(meeting);
        _dbContext.MeetingSummaries.Add(summary);
        await _dbContext.SaveChangesAsync();

        var status = await _sut.GetEmailStatusAsync(meeting.Id, CancellationToken.None);
        status.CanSend.Should().BeTrue();
        status.CanPreview.Should().BeTrue();

        var emailLog = new EmailLog { Id = Guid.NewGuid(), MeetingId = meeting.Id, Status = EmailDeliveryStatus.Sent, EmailType = EmailType.MeetingSummary, CreatedAt = DateTime.UtcNow };
        _dbContext.EmailLogs.Add(emailLog);
        await _dbContext.SaveChangesAsync();

        var status2 = await _sut.GetEmailStatusAsync(meeting.Id, CancellationToken.None);
        status2.CanSend.Should().BeFalse();
        status2.HasSentEmail.Should().BeTrue();
    }
    
    [Fact]
    public async Task StoreEmailBody_False_Davranisi()
    {
        _emailSettings.StoreEmailBody = false; // Adjusting for this test
        var user = new User { Id = _userId, Email = "test@test.com", FirstName = "T", LastName = "T" };
        var meeting = new Meeting { Id = Guid.NewGuid(), Title = "M", OrganizerUserId = _userId, Status = MeetingStatus.Approved };
        var emailLog = new EmailLog { Id = Guid.NewGuid(), MeetingId = meeting.Id, BodyHtml = "SECRET_HTML", BodyText = "SECRET_TXT", CreatedAt = DateTime.UtcNow };
        
        _dbContext.Users.Add(user);
        _dbContext.Meetings.Add(meeting);
        _dbContext.EmailLogs.Add(emailLog);
        await _dbContext.SaveChangesAsync();

        var detail = await _sut.GetEmailLogDetailAsync(meeting.Id, emailLog.Id, CancellationToken.None);
        detail.BodyHtml.Should().BeNull();
        detail.BodyText.Should().BeNull();
    }
    
    [Fact]
    public async Task StoreEmailBody_True_Davranisi()
    {
        _emailSettings.StoreEmailBody = true; // Adjusting for this test
        var user = new User { Id = _userId, Email = "test@test.com", FirstName = "T", LastName = "T" };
        var meeting = new Meeting { Id = Guid.NewGuid(), Title = "M", OrganizerUserId = _userId, Status = MeetingStatus.Approved };
        var emailLog = new EmailLog { Id = Guid.NewGuid(), MeetingId = meeting.Id, BodyHtml = "SECRET_HTML", BodyText = "SECRET_TXT", CreatedAt = DateTime.UtcNow };
        
        _dbContext.Users.Add(user);
        _dbContext.Meetings.Add(meeting);
        _dbContext.EmailLogs.Add(emailLog);
        await _dbContext.SaveChangesAsync();

        var detail = await _sut.GetEmailLogDetailAsync(meeting.Id, emailLog.Id, CancellationToken.None);
        detail.BodyHtml.Should().Be("SECRET_HTML");
        detail.BodyText.Should().Be("SECRET_TXT");
    }

    [Fact]
    public async Task EmailSent_Toplantinin_Tekrar_Gonderilememesi()
    {
        var user = new User { Id = _userId, Email = "test@test.com", FirstName = "T", LastName = "T" };
        var meeting = new Meeting { Id = Guid.NewGuid(), Title = "M", OrganizerUserId = _userId, Status = MeetingStatus.EmailSent };
        var summary = new MeetingSummary { Id = Guid.NewGuid(), MeetingId = meeting.Id, Version = 1, IsApproved = true };
        _dbContext.Users.Add(user);
        _dbContext.Meetings.Add(meeting);
        _dbContext.MeetingSummaries.Add(summary);
        await _dbContext.SaveChangesAsync();

        await _sut.Invoking(s => s.SendMeetingEmailAsync(meeting.Id, new SendMeetingEmailRequest(), CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Özet henüz onaylanmamış veya onayı düşmüş.*");
    }

    [Fact]
    public async Task HtmlEncode_Ve_ScriptGuvenligi()
    {
        // For this test, use the real template service
        var optionsMock2 = new Mock<IOptions<EmailSettings>>();
        optionsMock2.Setup(o => o.Value).Returns(_emailSettings);
        var realTemplateService = new MeetingEmailTemplateService(optionsMock2.Object);
        var meeting = new Meeting { Title = "<script>alert(1)</script>" };
        var summary = new MeetingSummary { ExecutiveSummary = "XSS" };
        
        var message = await realTemplateService.GeneratePreviewEmailAsync(
            meeting, summary, new List<ActionItem>(), new List<MeetingTopic>(), new List<MeetingDecision>(), 
            new List<OpenIssue>(), new List<MeetingParticipant>(), null, null, null, 
            false, false, false, false, false, false, false, CancellationToken.None);
            
        message.HtmlBody.Should().NotContain("<script>");
        message.HtmlBody.Should().Contain("&lt;script&gt;alert(1)&lt;/script&gt;");
    }

    [Fact]
    public async Task ActionStatus_Degisikliginin_ApprovalHash_Bozmamasi()
    {
        var user = new User { Id = _userId, Email = "test@test.com", FirstName = "T", LastName = "T" };
        var meeting = new Meeting { Id = Guid.NewGuid(), Title = "M", OrganizerUserId = _userId, Status = MeetingStatus.Approved };
        var summary = new MeetingSummary { Id = Guid.NewGuid(), MeetingId = meeting.Id, Version = 1, IsApproved = true };
        var action = new ActionItem { Id = Guid.NewGuid(), MeetingId = meeting.Id, MeetingSummaryId = summary.Id, Status = ActionItemStatus.Open, Description = "D" };
        
        _dbContext.Users.Add(user);
        _dbContext.Meetings.Add(meeting);
        _dbContext.MeetingSummaries.Add(summary);
        _dbContext.ActionItems.Add(action);
        await _dbContext.SaveChangesAsync();

        _mockMutationService.Setup(m => m.GetActiveSummaryForMutationAsync(meeting.Id, It.IsAny<int?>(), It.IsAny<int?>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((meeting, summary));

        await _actionService.UpdateActionItemStatusAsync(meeting.Id, action.Id, new TurkcellMeetingAssistant.Application.DTOs.Meetings.Summary.UpdateActionItemStatusRequest { Status = ActionItemStatus.Completed }, CancellationToken.None);

        var dbSummary = await _dbContext.MeetingSummaries.FindAsync(summary.Id);
        dbSummary!.IsApproved.Should().BeTrue();
        dbSummary.ManualRevisionNumber.Should().Be(0); // Assuming it starts at 0 and doesn't increment
    }

    [Fact]
    public async Task ActionDescription_Degisikliginin_Gonderimi_Engellemesi()
    {
        var user = new User { Id = _userId, Email = "test@test.com", FirstName = "T", LastName = "T" };
        var meeting = new Meeting { Id = Guid.NewGuid(), Title = "M", OrganizerUserId = _userId, Status = MeetingStatus.Approved };
        var summary = new MeetingSummary { Id = Guid.NewGuid(), MeetingId = meeting.Id, Version = 1, IsApproved = true, ManualRevisionNumber = 0 };
        var action = new ActionItem { Id = Guid.NewGuid(), MeetingId = meeting.Id, MeetingSummaryId = summary.Id, Status = ActionItemStatus.Open, Description = "Old" };
        
        _dbContext.Users.Add(user);
        _dbContext.Meetings.Add(meeting);
        _dbContext.MeetingSummaries.Add(summary);
        _dbContext.ActionItems.Add(action);
        await _dbContext.SaveChangesAsync();

        _mockMutationService.Setup(m => m.GetActiveSummaryForMutationAsync(meeting.Id, It.IsAny<int?>(), It.IsAny<int?>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((meeting, summary));

        // Simulate MutationService behavior for ActionItemUpdated
        _mockMutationService.Setup(m => m.MutateSummary(meeting, summary, "ActionItemUpdated", It.IsAny<string?>()))
            .Callback<Meeting, MeetingSummary, string, string?>((m, s, reason, changedFields) =>
            {
                s.ManualRevisionNumber++;
                s.IsApproved = false;
                m.Status = MeetingStatus.WaitingForApproval;
            });

        await _actionService.UpdateActionItemAsync(meeting.Id, action.Id, new TurkcellMeetingAssistant.Application.DTOs.Meetings.Summary.UpdateActionItemRequest { Description = "New" }, CancellationToken.None);

        // Now email send should fail
        await _sut.Invoking(s => s.SendMeetingEmailAsync(meeting.Id, new SendMeetingEmailRequest(), CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Özet henüz onaylanmamış veya onayı düşmüş.*");
    }
}
