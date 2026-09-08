using FluentValidation;

namespace HRM.OrganizationService.Application;

public class SaveDepartmentRequestValidator : AbstractValidator<SaveDepartmentRequest>
{
    public SaveDepartmentRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30)
            .Matches("^[A-Z0-9_-]+$").WithMessage("Use uppercase letters, digits, dash or underscore.");
    }
}

public class SaveDesignationRequestValidator : AbstractValidator<SaveDesignationRequest>
{
    public SaveDesignationRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Level).InclusiveBetween(1, 10);
    }
}

public class SaveBranchRequestValidator : AbstractValidator<SaveBranchRequest>
{
    public SaveBranchRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
    }
}

public class SaveHolidayRequestValidator : AbstractValidator<SaveHolidayRequest>
{
    public SaveHolidayRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Date).NotEmpty();
    }
}
