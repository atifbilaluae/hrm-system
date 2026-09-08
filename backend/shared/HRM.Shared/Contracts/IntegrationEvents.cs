namespace HRM.Shared.Contracts;

/// <summary>
/// Integration events are the PUBLIC contract between microservices.
/// They travel over RabbitMQ. Never share EF entities between services - share these.
/// </summary>
public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
    public abstract string EventName { get; }
}

public record EmployeeCreatedEvent : IntegrationEvent
{
    public override string EventName => EventNames.EmployeeCreated;
    public Guid EmployeeId { get; init; }
    public string EmployeeCode { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public Guid? DepartmentId { get; init; }
    public Guid? ManagerId { get; init; }
    public DateTime JoiningDate { get; init; }
}

public record EmployeeUpdatedEvent : IntegrationEvent
{
    public override string EventName => EventNames.EmployeeUpdated;
    public Guid EmployeeId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public Guid? DepartmentId { get; init; }
}

public record EmployeeTerminatedEvent : IntegrationEvent
{
    public override string EventName => EventNames.EmployeeTerminated;
    public Guid EmployeeId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public DateTime TerminationDate { get; init; }
}

public record DepartmentUpdatedEvent : IntegrationEvent
{
    public override string EventName => EventNames.DepartmentUpdated;
    public Guid DepartmentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public bool IsDeleted { get; init; }
}

public record AttendanceRecordedEvent : IntegrationEvent
{
    public override string EventName => EventNames.AttendanceRecorded;
    public Guid AttendanceId { get; init; }
    public Guid EmployeeId { get; init; }
    public DateTime WorkDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public int WorkedMinutes { get; init; }
    public int OvertimeMinutes { get; init; }
}

public record LeaveRequestedEvent : IntegrationEvent
{
    public override string EventName => EventNames.LeaveRequested;
    public Guid LeaveRequestId { get; init; }
    public Guid EmployeeId { get; init; }
    public Guid? ApproverId { get; init; }
    public string LeaveType { get; init; } = string.Empty;
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
}

public record LeaveDecisionEvent : IntegrationEvent
{
    public override string EventName => Approved ? EventNames.LeaveApproved : EventNames.LeaveRejected;
    public Guid LeaveRequestId { get; init; }
    public Guid EmployeeId { get; init; }
    public bool Approved { get; init; }
    public string? Comments { get; init; }
}

public record PayrollProcessedEvent : IntegrationEvent
{
    public override string EventName => EventNames.PayrollProcessed;
    public Guid PayrollRunId { get; init; }
    public int Month { get; init; }
    public int Year { get; init; }
    public int EmployeeCount { get; init; }
    public decimal TotalNetAmount { get; init; }
}

public static class EventNames
{
    public const string EmployeeCreated    = "employee.created";
    public const string EmployeeUpdated    = "employee.updated";
    public const string EmployeeTerminated = "employee.terminated";
    public const string LeaveRequested     = "leave.requested";
    public const string LeaveApproved      = "leave.approved";
    public const string LeaveRejected      = "leave.rejected";
    public const string AttendanceRecorded = "attendance.recorded";
    public const string DepartmentUpdated  = "department.updated";
    public const string PayrollProcessed   = "payroll.processed";
}
