using FluentValidation;

namespace HRM.LeaveService.Application;

public class CreateLeaveRequestValidator : AbstractValidator<CreateLeaveRequest>
{
    public CreateLeaveRequestValidator()
    {
        RuleFor(x => x.LeaveTypeId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500)
            .WithMessage("Tell your manager why you need the time off.");
        RuleFor(x => x.FromDate).NotEmpty();
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("The end date must be on or after the start date.");
        RuleFor(x => x.FromDate)
            .GreaterThan(DateTime.UtcNow.Date.AddDays(-30))
            .WithMessage("Backdated leave older than 30 days needs an HR adjustment instead.");
    }
}

public class SaveLeaveTypeRequestValidator : AbstractValidator<SaveLeaveTypeRequest>
{
    public SaveLeaveTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20)
            .Matches("^[A-Z0-9_-]+$").WithMessage("Use uppercase letters, digits, dash or underscore.");
        RuleFor(x => x.AnnualQuotaDays).InclusiveBetween(0, 365);
        RuleFor(x => x.MaxConsecutiveDays).InclusiveBetween(1, 365);
    }
}
