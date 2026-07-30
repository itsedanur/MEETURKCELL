using FluentValidation;
using TurkcellMeetingAssistant.Application.DTOs.Meetings;

namespace TurkcellMeetingAssistant.Application.Validators;

public class UpdateParticipantRequestValidator : AbstractValidator<UpdateParticipantRequest>
{
    public UpdateParticipantRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Ad soyad boş olamaz.")
            .MaximumLength(200).WithMessage("Ad soyad en fazla 200 karakter olabilir.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta boş olamaz.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .MaximumLength(200).WithMessage("E-posta adresi en fazla 200 karakter olabilir.");

        RuleFor(x => x.Department)
            .MaximumLength(200).WithMessage("Departman en fazla 200 karakter olabilir.");

        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Ünvan en fazla 200 karakter olabilir.");
    }
}
