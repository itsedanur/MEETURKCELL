using FluentValidation;
using TurkcellMeetingAssistant.Application.DTOs.Meetings.Analysis;

namespace TurkcellMeetingAssistant.Application.Validators;

public class MeetingAnalysisResultValidator : AbstractValidator<MeetingAnalysisResult>
{
    public MeetingAnalysisResultValidator()
    {
        RuleFor(x => x.ExecutiveSummary)
            .NotEmpty().WithMessage("Özet alanı boş olamaz.")
            .MaximumLength(5000).WithMessage("Özet alanı 5000 karakterden uzun olamaz.");

        RuleForEach(x => x.Topics).SetValidator(new AiMeetingTopicResultValidator());
        RuleForEach(x => x.Decisions).SetValidator(new AiMeetingDecisionResultValidator());
        RuleForEach(x => x.ActionItems).SetValidator(new AiActionItemResultValidator());
    }
}

public class AiMeetingTopicResultValidator : AbstractValidator<AiMeetingTopicResult>
{
    public AiMeetingTopicResultValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Konu başlığı boş olamaz.");
    }
}

public class AiMeetingDecisionResultValidator : AbstractValidator<AiMeetingDecisionResult>
{
    public AiMeetingDecisionResultValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Karar açıklaması boş olamaz.");

        RuleFor(x => x.ConfidenceScore)
            .InclusiveBetween(0, 1).WithMessage("Güven skoru 0 ile 1 arasında olmalıdır.");
    }
}

public class AiActionItemResultValidator : AbstractValidator<AiActionItemResult>
{
    public AiActionItemResultValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Aksiyon açıklaması boş olamaz.");

        RuleFor(x => x.Priority)
            .Must(BeAValidPriority).WithMessage("Geçerli bir öncelik (Priority) değeri belirtilmeli (Low, Medium, High, Critical).");

        RuleFor(x => x.ConfidenceScore)
            .InclusiveBetween(0, 1).WithMessage("Güven skoru 0 ile 1 arasında olmalıdır.");
            
        RuleFor(x => x.OwnerEmail)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.OwnerEmail))
            .WithMessage("Sorumlu e-posta adresi geçerli bir formatta olmalıdır.");
    }

    private bool BeAValidPriority(string priority)
    {
        return priority is "Low" or "Medium" or "High" or "Critical";
    }
}
