using FluentValidation;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Email;

namespace TurkcellMeetingAssistant.Application.Validators;

public class EmailRecipientRequestValidator : AbstractValidator<EmailRecipientRequest>
{
    public EmailRecipientRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta adresi boş olamaz.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");
            
        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("İsim en fazla 100 karakter olabilir.");
    }
}

public class SendMeetingEmailRequestValidator : AbstractValidator<SendMeetingEmailRequest>
{
    public SendMeetingEmailRequestValidator()
    {
        RuleFor(x => x.SubjectOverride)
            .MaximumLength(200).WithMessage("Konu en fazla 200 karakter olabilir.");
            
        RuleForEach(x => x.AdditionalTo).SetValidator(new EmailRecipientRequestValidator());
        RuleForEach(x => x.Cc).SetValidator(new EmailRecipientRequestValidator());
    }
}

public class SendTestEmailRequestValidator : AbstractValidator<SendTestEmailRequest>
{
    public SendTestEmailRequestValidator()
    {
        RuleFor(x => x.ToEmail)
            .NotEmpty().WithMessage("E-posta adresi boş olamaz.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");
    }
}
