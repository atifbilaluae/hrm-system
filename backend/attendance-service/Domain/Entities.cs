namespace HRM.AttendanceService.Domain;

public enum AttendanceStatus { Present = 1, Late = 2, Absent = 3, HalfDay = 4, OnLeave = 5, Holiday = 6, WeekOff = 7 }
public enum CorrectionStatus { Pending = 1, Approved = 2, Rejected = 3 }

/// <summary>
/// Shift rules. One default policy ships; a policy can later be attached per
/// department or branch without changing any of the calculation code.
/// </summary>
public class AttendancePolicy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Default shift";
    public TimeSpan ShiftStart { get; set; } = new(9, 0, 0);
    public TimeSpan ShiftEnd { get; set; } = new(18, 0, 0);
    /// <summary>Minutes after ShiftStart that still count as on time.</summary>
    public int GraceMinutes { get; set; } = 15;
    public int StandardWorkMinutes { get; set; } = 480;      // 8 hours
    public int HalfDayThresholdMinutes { get; set; } = 240;  // under 4 hours = half day
    public int MinimumOvertimeMinutes { get; set; } = 30;    // ignore a few stray minutes
    public bool IsDefault { get; set; } = true;
}

/// <summary>
/// One row per employee per day. All the arithmetic lives here so it can be
/// unit tested without a database, a clock or an HTTP request.
/// </summary>
public class AttendanceRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public DateTime WorkDate { get; set; }
    public DateTime? CheckInUtc { get; set; }
    public DateTime? CheckOutUtc { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Absent;
    public int WorkedMinutes { get; set; }
    public int LateMinutes { get; set; }
    public int EarlyDepartureMinutes { get; set; }
    public int OvertimeMinutes { get; set; }
    public string? Notes { get; set; }
    public bool IsManualEntry { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public void CheckIn(DateTime whenUtc, AttendancePolicy policy)
    {
        if (CheckInUtc is not null)
            throw new InvalidOperationException("You have already checked in today.");

        CheckInUtc = whenUtc;
        var shiftStart = WorkDate.Date.Add(policy.ShiftStart);
        var minutesLate = (int)(whenUtc - shiftStart).TotalMinutes;

        LateMinutes = Math.Max(0, minutesLate - policy.GraceMinutes);
        Status = LateMinutes > 0 ? AttendanceStatus.Late : AttendanceStatus.Present;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void CheckOut(DateTime whenUtc, AttendancePolicy policy)
    {
        if (CheckInUtc is null)
            throw new InvalidOperationException("Check in before checking out.");
        if (CheckOutUtc is not null)
            throw new InvalidOperationException("You have already checked out today.");
        if (whenUtc <= CheckInUtc)
            throw new InvalidOperationException("Check-out must be after check-in.");

        CheckOutUtc = whenUtc;
        WorkedMinutes = (int)(whenUtc - CheckInUtc.Value).TotalMinutes;

        var shiftEnd = WorkDate.Date.Add(policy.ShiftEnd);
        EarlyDepartureMinutes = Math.Max(0, (int)(shiftEnd - whenUtc).TotalMinutes);

        var extra = WorkedMinutes - policy.StandardWorkMinutes;
        OvertimeMinutes = extra >= policy.MinimumOvertimeMinutes ? extra : 0;

        if (WorkedMinutes < policy.HalfDayThresholdMinutes)
            Status = AttendanceStatus.HalfDay;
        else if (LateMinutes > 0)
            Status = AttendanceStatus.Late;
        else
            Status = AttendanceStatus.Present;

        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ApplyCorrection(DateTime? checkIn, DateTime? checkOut, AttendancePolicy policy)
    {
        CheckInUtc = null;
        CheckOutUtc = null;
        WorkedMinutes = LateMinutes = EarlyDepartureMinutes = OvertimeMinutes = 0;
        Status = AttendanceStatus.Absent;
        IsManualEntry = true;

        if (checkIn is not null) CheckIn(checkIn.Value, policy);
        if (checkIn is not null && checkOut is not null) CheckOut(checkOut.Value, policy);
    }
}

public class AttendanceCorrectionRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public DateTime WorkDate { get; set; }
    public DateTime? RequestedCheckInUtc { get; set; }
    public DateTime? RequestedCheckOutUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
    public CorrectionStatus Status { get; set; } = CorrectionStatus.Pending;
    public Guid? ReviewedByEmployeeId { get; set; }
    public string? ReviewComments { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }

    public void Decide(bool approve, Guid? reviewerId, string? comments)
    {
        if (Status != CorrectionStatus.Pending)
            throw new InvalidOperationException("This correction request has already been reviewed.");

        Status = approve ? CorrectionStatus.Approved : CorrectionStatus.Rejected;
        ReviewedByEmployeeId = reviewerId;
        ReviewComments = comments;
        ReviewedAtUtc = DateTime.UtcNow;
    }
}
