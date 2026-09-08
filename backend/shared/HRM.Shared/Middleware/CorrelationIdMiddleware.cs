using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace HRM.Shared.Middleware;

/// <summary>
/// Every request gets an X-Correlation-Id. It is pushed into the Serilog context so
/// one user action can be traced across gateway -> service -> RabbitMQ -> consumer.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : Guid.NewGuid().ToString();

        context.Items[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
