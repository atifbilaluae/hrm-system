using FluentValidation;

namespace HRM.AttendanceService.Application;

public class CreateCorrectionRequestValidator : AbstractValidator<CreateCorrectionRequest>
{
    public CreateCorrectionRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500)
            .WithMessage("Explain why the record needs correcting.");
        RuleFor(x => x.WorkDate)
            .LessThanOrEqualTo(DateTime.UtcNow.Date).WithMessage("You cannot correct a future date.")
            .GreaterThan(DateTime.UtcNow.Date.AddDays(-60)).WithMessage("Corrections are limited to the last 60 days.");
        RuleFor(x => x)
            .Must(x => x.RequestedCheckInUtc is not null || x.RequestedCheckOutUtc is not null)
            .WithMessage("Provide at least a check-in or a check-out time.");
    }
}

public class ManualAttendanceRequestValidator : AbstractValidator<ManualAttendanceRequest>
{
    public ManualAttendanceRequestValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.WorkDate).LessThanOrEqualTo(DateTime.UtcNow.Date);
    }
}
