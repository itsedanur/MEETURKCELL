using FluentValidation.TestHelper;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Email;
using TurkcellMeetingAssistant.Application.Validators;
using Xunit;

namespace TurkcellMeetingAssistant.UnitTests.Validators;

public class EmailRequestsValidatorTests
{
    private readonly EmailRecipientRequestValidator _recipientValidator = new();
    private readonly SendMeetingEmailRequestValidator _sendValidator = new();
    private readonly SendTestEmailRequestValidator _testValidator = new();

    [Fact]
    public void EmailRecipientRequestValidator_ShouldHaveError_WhenEmailIsEmpty()
    {
        var model = new EmailRecipientRequest { Email = "" };
        var result = _recipientValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void EmailRecipientRequestValidator_ShouldHaveError_WhenEmailIsInvalid()
    {
        var model = new EmailRecipientRequest { Email = "invalid-email" };
        var result = _recipientValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void EmailRecipientRequestValidator_ShouldNotHaveError_WhenEmailIsValid()
    {
        var model = new EmailRecipientRequest { Email = "test@example.com" };
        var result = _recipientValidator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void SendTestEmailRequestValidator_ShouldHaveError_WhenEmailIsInvalid()
    {
        var model = new SendTestEmailRequest { ToEmail = "invalid" };
        var result = _testValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ToEmail);
    }

    [Fact]
    public void SendMeetingEmailRequestValidator_ShouldHaveError_WhenSubjectIsTooLong()
    {
        var model = new SendMeetingEmailRequest { SubjectOverride = new string('a', 201) };
        var result = _sendValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.SubjectOverride);
    }
}
