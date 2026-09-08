using FluentValidation;

namespace HRM.EmployeeService.Application;

public class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    public CreateEmployeeRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.DateOfBirth)
            .LessThan(DateTime.UtcNow.AddYears(-16)).WithMessage("Employee must be at least 16 years old.");
        RuleFor(x => x.JoiningDate)
            .LessThanOrEqualTo(DateTime.UtcNow.AddMonths(6)).WithMessage("Joining date is too far in the future.");
        RuleFor(x => x.BasicSalary).GreaterThan(0).WithMessage("Basic salary must be greater than zero.");
    }
}

public class UpdateEmployeeRequestValidator : AbstractValidator<UpdateEmployeeRequest>
{
    public UpdateEmployeeRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BasicSalary).GreaterThanOrEqualTo(0);
    }
}
