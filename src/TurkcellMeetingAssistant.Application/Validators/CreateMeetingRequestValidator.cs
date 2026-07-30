using FluentValidation;
using TurkcellMeetingAssistant.Application.DTOs.Meetings;

namespace TurkcellMeetingAssistant.Application.Validators;

public class CreateMeetingRequestValidator : AbstractValidator<CreateMeetingRequest>
{
    public CreateMeetingRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Toplantı başlığı boş olamaz.")
            .MaximumLength(200).WithMessage("Toplantı başlığı en fazla 200 karakter olabilir.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Toplantı açıklaması en fazla 2000 karakter olabilir.");

        RuleFor(x => x.MeetingDate)
            .NotEmpty().WithMessage("Toplantı tarihi geçerli olmalıdır.");

        RuleFor(x => x)
            .Must(x => !(x.StartTime.HasValue ^ x.EndTime.HasValue))
            .WithMessage("Sadece bir saat alanı verilemez, hem başlangıç hem bitiş saati belirtilmelidir.")
            .DependentRules(() =>
            {
                RuleFor(x => x.EndTime)
                    .GreaterThan(x => x.StartTime)
                    .When(x => x.StartTime.HasValue && x.EndTime.HasValue)
                    .WithMessage("Bitiş saati başlangıç saatinden önce veya aynı olamaz.");
            });
    }
}
