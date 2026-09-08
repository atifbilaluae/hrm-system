using FluentAssertions;
using HRM.AttendanceService.Domain;
using Xunit;

namespace HRM.AttendanceService.Tests;

/// <summary>
/// Shift arithmetic is the part of attendance that people argue about, so it is
/// tested exhaustively and kept free of any database or clock dependency.
/// </summary>
public class AttendanceRecordTests
{
    private static readonly DateTime WorkDate = new(2026, 3, 2);   // a Monday
    private static readonly AttendancePolicy Policy = new();       // 09:00-18:00, 15 min grace, 480 min standard

    private static AttendanceRecord NewRecord() => new()
    {
        EmployeeId = Guid.NewGuid(),
        WorkDate = WorkDate
    };

    private static DateTime At(int hour, int minute) => WorkDate.AddHours(hour).AddMinutes(minute);

    [Fact]
    public void Arriving_before_the_shift_starts_is_present()
    {
        var record = NewRecord();

        record.CheckIn(At(8, 50), Policy);

        record.Status.Should().Be(AttendanceStatus.Present);
        record.LateMinutes.Should().Be(0);
    }

    [Fact]
    public void Arriving_inside_the_grace_period_is_still_present()
    {
        var record = NewRecord();

        record.CheckIn(At(9, 12), Policy);   // 12 minutes late, grace is 15

        record.Status.Should().Be(AttendanceStatus.Present);
        record.LateMinutes.Should().Be(0);
    }

    [Fact]
    public void Arriving_after_the_grace_period_is_late_by_the_excess_only()
    {
        var record = NewRecord();

        record.CheckIn(At(9, 45), Policy);   // 45 minutes late, minus 15 grace

        record.Status.Should().Be(AttendanceStatus.Late);
        record.LateMinutes.Should().Be(30);
    }

    [Fact]
    public void Checking_in_twice_is_rejected()
    {
        var record = NewRecord();
        record.CheckIn(At(9, 0), Policy);

        var act = () => record.CheckIn(At(9, 30), Policy);

        act.Should().Throw<InvalidOperationException>().WithMessage("*already checked in*");
    }

    [Fact]
    public void Checking_out_without_checking_in_is_rejected()
    {
        var act = () => NewRecord().CheckOut(At(18, 0), Policy);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Check in before*");
    }

    [Fact]
    public void A_full_day_records_worked_minutes_and_no_overtime()
    {
        var record = NewRecord();
        record.CheckIn(At(9, 0), Policy);

        record.CheckOut(At(17, 0), Policy);   // exactly 8 hours

        record.WorkedMinutes.Should().Be(480);
        record.OvertimeMinutes.Should().Be(0);
        record.Status.Should().Be(AttendanceStatus.Present);
    }

    [Fact]
    public void Overtime_below_the_minimum_is_ignored()
    {
        var record = NewRecord();
        record.CheckIn(At(9, 0), Policy);

        record.CheckOut(At(17, 20), Policy);   // 20 extra minutes, minimum is 30

        record.OvertimeMinutes.Should().Be(0);
    }

    [Fact]
    public void Overtime_above_the_minimum_is_recorded_in_full()
    {
        var record = NewRecord();
        record.CheckIn(At(9, 0), Policy);

        record.CheckOut(At(19, 0), Policy);   // 10 hours

        record.WorkedMinutes.Should().Be(600);
        record.OvertimeMinutes.Should().Be(120);
    }

    [Fact]
    public void Leaving_before_the_half_day_threshold_marks_a_half_day()
    {
        var record = NewRecord();
        record.CheckIn(At(9, 0), Policy);

        record.CheckOut(At(12, 0), Policy);   // 3 hours, threshold is 4

        record.Status.Should().Be(AttendanceStatus.HalfDay);
        record.WorkedMinutes.Should().Be(180);
    }

    [Fact]
    public void Leaving_before_the_shift_ends_records_early_departure_minutes()
    {
        var record = NewRecord();
        record.CheckIn(At(9, 0), Policy);

        record.CheckOut(At(16, 30), Policy);   // shift ends at 18:00

        record.EarlyDepartureMinutes.Should().Be(90);
    }

    [Fact]
    public void Checking_out_before_checking_in_is_rejected()
    {
        var record = NewRecord();
        record.CheckIn(At(10, 0), Policy);

        var act = () => record.CheckOut(At(9, 0), Policy);

        act.Should().Throw<InvalidOperationException>().WithMessage("*after check-in*");
    }

    [Fact]
    public void A_correction_recalculates_the_whole_day_from_scratch()
    {
        var record = NewRecord();
        record.CheckIn(At(10, 30), Policy);    // late
        record.CheckOut(At(13, 0), Policy);    // half day

        record.ApplyCorrection(At(9, 0), At(18, 30), Policy);

        record.Status.Should().Be(AttendanceStatus.Present);
        record.LateMinutes.Should().Be(0);
        record.WorkedMinutes.Should().Be(570);
        record.OvertimeMinutes.Should().Be(90);
        record.IsManualEntry.Should().BeTrue();
    }
}

public class AttendanceCorrectionTests
{
    private static AttendanceCorrectionRequest NewCorrection() => new()
    {
        EmployeeId = Guid.NewGuid(),
        WorkDate = new DateTime(2026, 3, 2),
        Reason = "Badge reader was offline"
    };

    [Fact]
    public void Approving_stores_the_reviewer_and_timestamp()
    {
        var correction = NewCorrection();
        var reviewer = Guid.NewGuid();

        correction.Decide(true, reviewer, "Verified with security");

        correction.Status.Should().Be(CorrectionStatus.Approved);
        correction.ReviewedByEmployeeId.Should().Be(reviewer);
        correction.ReviewedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void A_correction_cannot_be_reviewed_twice()
    {
        var correction = NewCorrection();
        correction.Decide(false, Guid.NewGuid(), "No evidence");

        var act = () => correction.Decide(true, Guid.NewGuid(), null);

        act.Should().Throw<InvalidOperationException>().WithMessage("*already been reviewed*");
    }
}
