namespace HRM.LeaveService.Domain;

public enum LeaveRequestStatus { Pending = 1, Approved = 2, Rejected = 3, Cancelled = 4 }

public class LeaveType
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int AnnualQuotaDays { get; set; }
    public bool IsPaid { get; set; } = true;
    public bool RequiresApproval { get; set; } = true;
    public int MaxConsecutiveDays { get; set; } = 30;
    /// <summary>Days of notice normally expected before the start date.</summary>
    public int MinimumNoticeDays { get; set; }
    public bool CarryForward { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// One row per employee per leave type per year. Entitled - Used - Pending = Available.
/// Pending is held separately so two overlapping requests cannot both fit in the balance.
/// </summary>
public class LeaveBalance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;
    public int Year { get; set; }
    public decimal EntitledDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal PendingDays { get; set; }
    public decimal AvailableDays => EntitledDays - UsedDays - PendingDays;

    public void Hold(decimal days)
    {
        if (days > AvailableDays)
            throw new InvalidOperationException(
                $"Only {AvailableDays:0.#} day(s) of {LeaveType?.Name ?? "this leave"} remain, but {days:0.#} were requested.");
        PendingDays += days;
    }

    public void Consume(decimal days)
    {
        PendingDays = Math.Max(0, PendingDays - days);
        UsedDays += days;
    }

    public void Release(decimal days) => PendingDays = Math.Max(0, PendingDays - days);
}

public class LeaveRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal TotalDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;
    public Guid? ApproverEmployeeId { get; set; }
    public string? DecisionComments { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public void Decide(bool approve, Guid? approverId, string? comments)
    {
        if (Status != LeaveRequestStatus.Pending)
            throw new InvalidOperationException("This request has already been decided.");
        if (approverId is not null && approverId == EmployeeId)
            throw new InvalidOperationException("You cannot approve your own leave request.");

        Status = approve ? LeaveRequestStatus.Approved : LeaveRequestStatus.Rejected;
        ApproverEmployeeId = approverId;
        DecisionComments = comments;
        DecidedAtUtc = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == LeaveRequestStatus.Approved && FromDate.Date <= DateTime.UtcNow.Date)
            throw new InvalidOperationException("Leave that has already started cannot be cancelled here.");
        if (Status is LeaveRequestStatus.Rejected or LeaveRequestStatus.Cancelled)
            throw new InvalidOperationException("This request is already closed.");

        Status = LeaveRequestStatus.Cancelled;
        DecidedAtUtc = DateTime.UtcNow;
    }
}

/// <summary>
/// Working-day arithmetic, kept in one place. Weekends are excluded; public
/// holidays are passed in by the caller, which reads them from Organization Service.
/// </summary>
public static class LeaveCalculator
{
    public static decimal CountWorkingDays(DateTime from, DateTime to, IReadOnlyCollection<DateTime>? holidays = null)
    {
        if (to.Date < from.Date)
            throw new ArgumentException("The end date cannot be before the start date.");

        var holidaySet = holidays?.Select(h => h.Date).ToHashSet() ?? [];
        var days = 0;

        for (var cursor = from.Date; cursor <= to.Date; cursor = cursor.AddDays(1))
        {
            if (cursor.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            if (holidaySet.Contains(cursor)) continue;
            days++;
        }

        return days;
    }

    public static bool Overlaps(DateTime aFrom, DateTime aTo, DateTime bFrom, DateTime bTo) =>
        aFrom.Date <= bTo.Date && bFrom.Date <= aTo.Date;
}
