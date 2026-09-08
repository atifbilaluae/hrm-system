using System.Text.Json;
using HRM.NotificationService.Domain;
using HRM.NotificationService.Infrastructure;
using HRM.Shared.Contracts;
using HRM.Shared.Messaging;
using Microsoft.Extensions.Options;

namespace HRM.NotificationService.Application;

/// <summary>
/// The consuming half of the event-driven flow. Employee/Leave/Payroll services publish;
/// this service turns those events into user-facing notifications.
/// </summary>
public class HrmEventConsumer(
    IOptions<RabbitMqSettings> options,
    IServiceScopeFactory scopeFactory,
    ILogger<HrmEventConsumer> logger) : RabbitMqConsumerBase(options, logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    protected override string QueueName => "notification-service.events";

    protected override string[] RoutingKeys =>
    [
        EventNames.EmployeeCreated,
        EventNames.EmployeeTerminated,
        EventNames.LeaveRequested,
        EventNames.LeaveApproved,
        EventNames.LeaveRejected,
        EventNames.PayrollProcessed
    ];

    protected override async Task HandleAsync(string eventName, string json, CancellationToken ct)
    {
        // BackgroundService is a singleton; DbContext is scoped -> create a scope per message.
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var notification = eventName switch
        {
            EventNames.EmployeeCreated => Build(JsonSerializer.Deserialize<EmployeeCreatedEvent>(json, Json)!),
            EventNames.EmployeeTerminated => Build(JsonSerializer.Deserialize<EmployeeTerminatedEvent>(json, Json)!),
            EventNames.LeaveRequested => Build(JsonSerializer.Deserialize<LeaveRequestedEvent>(json, Json)!),
            EventNames.LeaveApproved or EventNames.LeaveRejected =>
                Build(JsonSerializer.Deserialize<LeaveDecisionEvent>(json, Json)!),
            EventNames.PayrollProcessed => Build(JsonSerializer.Deserialize<PayrollProcessedEvent>(json, Json)!),
            _ => null
        };

        if (notification is null) return;

        db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct);

        if (notification.Channel == NotificationChannel.Email)
            await email.SendAsync("hr@hrm.local", notification.Title, notification.Message, ct);

        Logger.LogInformation("Stored notification for {EventName}", eventName);
    }

    private static Notification Build(EmployeeCreatedEvent e) => new()
    {
        RecipientEmployeeId = e.ManagerId,
        Title = "New employee onboarded",
        Message = $"{e.FullName} ({e.EmployeeCode}) joined on {e.JoiningDate:dd MMM yyyy}.",
        Type = NotificationType.Success,
        Link = $"/employees/{e.EmployeeId}",
        SourceEvent = e.EventName
    };

    private static Notification Build(EmployeeTerminatedEvent e) => new()
    {
        Title = "Employee exit recorded",
        Message = $"{e.FullName} was marked as terminated effective {e.TerminationDate:dd MMM yyyy}.",
        Type = NotificationType.Warning,
        SourceEvent = e.EventName
    };

    private static Notification Build(LeaveRequestedEvent e) => new()
    {
        RecipientEmployeeId = e.ApproverId,
        Title = "Leave request needs your approval",
        Message = $"{e.LeaveType} leave from {e.FromDate:dd MMM} to {e.ToDate:dd MMM} is awaiting your decision.",
        Type = NotificationType.Approval,
        Link = $"/leave/requests/{e.LeaveRequestId}",
        SourceEvent = e.EventName
    };

    private static Notification Build(LeaveDecisionEvent e) => new()
    {
        RecipientEmployeeId = e.EmployeeId,
        Title = e.Approved ? "Leave request approved" : "Leave request rejected",
        Message = e.Approved
            ? "Your leave request has been approved."
            : $"Your leave request has been rejected. {e.Comments}".Trim(),
        Type = e.Approved ? NotificationType.Success : NotificationType.Warning,
        Link = $"/leave/requests/{e.LeaveRequestId}",
        SourceEvent = e.EventName
    };

    private static Notification Build(PayrollProcessedEvent e) => new()
    {
        Title = "Payroll processed",
        Message = $"Payroll for {e.Month:D2}/{e.Year} is complete: {e.EmployeeCount} employees, net {e.TotalNetAmount:N0}.",
        Type = NotificationType.Info,
        Link = "/payroll",
        SourceEvent = e.EventName
    };
}
