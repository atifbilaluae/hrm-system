using FluentAssertions;
using HRM.LeaveService.Domain;
using Xunit;

namespace HRM.LeaveService.Tests;

public class LeaveCalculatorTests
{
    [Fact]
    public void Weekends_do_not_count_as_leave_days()
    {
        // Monday 2 Mar 2026 to Friday 6 Mar 2026 = 5 working days
        var days = LeaveCalculator.CountWorkingDays(new DateTime(2026, 3, 2), new DateTime(2026, 3, 6));

        days.Should().Be(5);
    }

    [Fact]
    public void A_range_spanning_a_weekend_skips_saturday_and_sunday()
    {
        // Friday 6 Mar to Monday 9 Mar = Friday + Monday only
        var days = LeaveCalculator.CountWorkingDays(new DateTime(2026, 3, 6), new DateTime(2026, 3, 9));

        days.Should().Be(2);
    }

    [Fact]
    public void Public_holidays_are_excluded_when_supplied()
    {
        var holidays = new[] { new DateTime(2026, 3, 23) };   // Pakistan Day, a Monday

        var days = LeaveCalculator.CountWorkingDays(new DateTime(2026, 3, 23), new DateTime(2026, 3, 27), holidays);

        days.Should().Be(4);
    }

    [Fact]
    public void A_weekend_only_range_counts_as_zero_days()
    {
        LeaveCalculator.CountWorkingDays(new DateTime(2026, 3, 7), new DateTime(2026, 3, 8))
            .Should().Be(0);
    }

    [Fact]
    public void An_end_date_before_the_start_date_is_rejected()
    {
        var act = () => LeaveCalculator.CountWorkingDays(new DateTime(2026, 3, 10), new DateTime(2026, 3, 1));

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("2026-03-02", "2026-03-06", "2026-03-05", "2026-03-10", true)]
    [InlineData("2026-03-02", "2026-03-06", "2026-03-09", "2026-03-13", false)]
    [InlineData("2026-03-02", "2026-03-06", "2026-03-06", "2026-03-06", true)]
    public void Overlaps_detects_clashing_ranges(string aFrom, string aTo, string bFrom, string bTo, bool expected)
    {
        LeaveCalculator.Overlaps(DateTime.Parse(aFrom), DateTime.Parse(aTo),
                                 DateTime.Parse(bFrom), DateTime.Parse(bTo))
            .Should().Be(expected);
    }
}

public class LeaveBalanceTests
{
    private static LeaveBalance Balance(decimal entitled = 20) => new()
    {
        EntitledDays = entitled,
        LeaveType = new LeaveType { Name = "Annual Leave" }
    };

    [Fact]
    public void Holding_days_reduces_what_is_available_without_using_them()
    {
        var balance = Balance();

        balance.Hold(5);

        balance.PendingDays.Should().Be(5);
        balance.UsedDays.Should().Be(0);
        balance.AvailableDays.Should().Be(15);
    }

    [Fact]
    public void Holding_more_than_available_is_rejected()
    {
        var balance = Balance(entitled: 3);

        var act = () => balance.Hold(5);

        act.Should().Throw<InvalidOperationException>().WithMessage("*3 day(s)*");
    }

    [Fact]
    public void Two_requests_cannot_both_spend_the_same_balance()
    {
        var balance = Balance(entitled: 10);
        balance.Hold(6);

        var act = () => balance.Hold(6);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Approving_turns_a_hold_into_used_days()
    {
        var balance = Balance();
        balance.Hold(4);

        balance.Consume(4);

        balance.PendingDays.Should().Be(0);
        balance.UsedDays.Should().Be(4);
        balance.AvailableDays.Should().Be(16);
    }

    [Fact]
    public void Rejecting_gives_the_held_days_back()
    {
        var balance = Balance();
        balance.Hold(4);

        balance.Release(4);

        balance.PendingDays.Should().Be(0);
        balance.AvailableDays.Should().Be(20);
    }
}

public class LeaveRequestTests
{
    private static LeaveRequest Request(Guid? employeeId = null) => new()
    {
        EmployeeId = employeeId ?? Guid.NewGuid(),
        FromDate = DateTime.UtcNow.Date.AddDays(7),
        ToDate = DateTime.UtcNow.Date.AddDays(9),
        TotalDays = 3,
        Reason = "Family commitment"
    };

    [Fact]
    public void Approving_records_the_approver_and_timestamp()
    {
        var request = Request();
        var approver = Guid.NewGuid();

        request.Decide(true, approver, "Enjoy");

        request.Status.Should().Be(LeaveRequestStatus.Approved);
        request.ApproverEmployeeId.Should().Be(approver);
        request.DecidedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void An_employee_cannot_approve_their_own_request()
    {
        var employeeId = Guid.NewGuid();
        var request = Request(employeeId);

        var act = () => request.Decide(true, employeeId, null);

        act.Should().Throw<InvalidOperationException>().WithMessage("*your own*");
    }

    [Fact]
    public void A_request_cannot_be_decided_twice()
    {
        var request = Request();
        request.Decide(true, Guid.NewGuid(), null);

        var act = () => request.Decide(false, Guid.NewGuid(), null);

        act.Should().Throw<InvalidOperationException>().WithMessage("*already been decided*");
    }

    [Fact]
    public void Pending_requests_can_be_cancelled()
    {
        var request = Request();

        request.Cancel();

        request.Status.Should().Be(LeaveRequestStatus.Cancelled);
    }

    [Fact]
    public void Leave_that_already_started_cannot_be_cancelled()
    {
        var request = Request();
        request.FromDate = DateTime.UtcNow.Date.AddDays(-1);
        request.Decide(true, Guid.NewGuid(), null);

        var act = () => request.Cancel();

        act.Should().Throw<InvalidOperationException>().WithMessage("*already started*");
    }
}
