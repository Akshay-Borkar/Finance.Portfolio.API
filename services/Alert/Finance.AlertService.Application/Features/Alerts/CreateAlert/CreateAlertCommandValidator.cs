using FluentValidation;

namespace Finance.AlertService.Application.Features.Alerts.CreateAlert;

public class CreateAlertCommandValidator : AbstractValidator<CreateAlertCommand>
{
    private static readonly string[] ValidConditions = ["Above", "Below"];

    public CreateAlertCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("{PropertyName} is required.");

        RuleFor(x => x.Ticker)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MaximumLength(10).WithMessage("{PropertyName} must not exceed 10 characters.");

        RuleFor(x => x.Condition)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .Must(c => ValidConditions.Contains(c)).WithMessage("{PropertyName} must be 'Above' or 'Below'.");

        RuleFor(x => x.TargetPrice)
            .GreaterThan(0).WithMessage("{PropertyName} must be greater than 0.");

        RuleFor(x => x.UserEmail)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .EmailAddress().WithMessage("{PropertyName} is not a valid email address.");
    }
}
