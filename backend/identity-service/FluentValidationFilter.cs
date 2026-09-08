using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HRM.IdentityService;

/// <summary>
/// Runs FluentValidation for any action argument that has a registered validator.
/// Failures throw ValidationException, which the shared middleware turns into a 400 with field errors.
/// </summary>
public class ValidationFilter(IServiceProvider provider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values.Where(a => a is not null))
        {
            var validatorType = typeof(IValidator<>).MakeGenericType(argument!.GetType());
            if (provider.GetService(validatorType) is not IValidator validator) continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument));
            if (!result.IsValid) throw new ValidationException(result.Errors);
        }
        await next();
    }
}
