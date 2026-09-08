using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HRM.LeaveService;

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
