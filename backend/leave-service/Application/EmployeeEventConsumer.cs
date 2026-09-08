using System.Text.Json;
using HRM.Shared.Contracts;
using HRM.Shared.Messaging;
using Microsoft.Extensions.Options;

namespace HRM.LeaveService.Application;

/// <summary>
/// When Employee Service announces a new hire, this service gives them leave
/// balances for the current year. No synchronous call between the two services -
/// onboarding still works if Leave Service is briefly down, it just catches up.
/// </summary>
public class EmployeeEventConsumer(
    IOptions<RabbitMqSettings> options,
    IServiceScopeFactory scopeFactory,
    ILogger<EmployeeEventConsumer> logger) : RabbitMqConsumerBase(options, logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    protected override string QueueName => "leave-service.employee-events";
    protected override string[] RoutingKeys => [EventNames.EmployeeCreated];

    protected override async Task HandleAsync(string eventName, string json, CancellationToken ct)
    {
        if (eventName != EventNames.EmployeeCreated) return;

        var @event = JsonSerializer.Deserialize<EmployeeCreatedEvent>(json, Json);
        if (@event is null) return;

        await using var scope = scopeFactory.CreateAsyncScope();
        var leave = scope.ServiceProvider.GetRequiredService<ILeaveService>();

        await leave.EnsureBalancesAsync(@event.EmployeeId, DateTime.UtcNow.Year, ct);
        Logger.LogInformation("Leave balances prepared for new hire {FullName}", @event.FullName);
    }
}
