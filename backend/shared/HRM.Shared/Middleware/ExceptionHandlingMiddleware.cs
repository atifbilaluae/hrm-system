using System.Text.Json;
using FluentValidation;
using HRM.Shared.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HRM.Shared.Middleware;

/// <summary>
/// One place that turns exceptions into consistent JSON. Controllers therefore never
/// need try/catch, and internal details never leak to the client.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items[CorrelationIdMiddleware.HeaderName]?.ToString();
            var response = ex switch
            {
                ValidationException v => new ApiErrorResponse
                {
                    Title = "Validation failed", Status = StatusCodes.Status400BadRequest,
                    Errors = v.Errors.GroupBy(e => e.PropertyName)
                                     .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                },
                NotFoundException n => new ApiErrorResponse
                    { Title = "Not found", Status = StatusCodes.Status404NotFound, Detail = n.Message },
                ConflictException c => new ApiErrorResponse
                    { Title = "Conflict", Status = StatusCodes.Status409Conflict, Detail = c.Message },
                DomainRuleException d => new ApiErrorResponse
                    { Title = "Business rule violated", Status = StatusCodes.Status422UnprocessableEntity, Detail = d.Message },
                UnauthorizedAccessException => new ApiErrorResponse
                    { Title = "Unauthorized", Status = StatusCodes.Status401Unauthorized },
                _ => new ApiErrorResponse
                    { Title = "Unexpected error", Status = StatusCodes.Status500InternalServerError,
                      Detail = "Something went wrong. Contact support with the correlation id." }
            };

            response.CorrelationId = correlationId;

            if (response.Status >= 500) logger.LogError(ex, "Unhandled exception");
            else logger.LogWarning("Handled {Type}: {Message}", ex.GetType().Name, ex.Message);

            context.Response.StatusCode = response.Status;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(response,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }
    }
}
