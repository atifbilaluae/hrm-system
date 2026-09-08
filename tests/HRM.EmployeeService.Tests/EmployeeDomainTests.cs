using FluentAssertions;
using HRM.EmployeeService.Domain;
using Xunit;

namespace HRM.EmployeeService.Tests;

/// <summary>
/// Domain rules are tested without a database or HTTP. That is the payoff of
/// keeping behaviour on the entity instead of in the controller.
/// </summary>
public class EmployeeDomainTests
{
    private static Employee NewEmployee(DateTime? joiningDate = null) => new()
    {
        FirstName = "Ayesha",
        LastName = "Khan",
        Email = "ayesha.khan@hrm.local",
        JoiningDate = joiningDate ?? new DateTime(2024, 1, 15),
        EmploymentStatus = EmploymentStatus.Active
    };

    [Fact]
    public void Terminate_sets_status_and_date()
    {
        var employee = NewEmployee();
        var terminationDate = new DateTime(2026, 3, 31);

        employee.Terminate(terminationDate);

        employee.EmploymentStatus.Should().Be(EmploymentStatus.Terminated);
        employee.TerminationDate.Should().Be(terminationDate);
        employee.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Terminate_rejects_a_date_before_joining()
    {
        var employee = NewEmployee(new DateTime(2025, 6, 1));

        var act = () => employee.Terminate(new DateTime(2025, 1, 1));

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*before the joining date*");
    }

    [Fact]
    public void Terminate_twice_is_rejected()
    {
        var employee = NewEmployee();
        employee.Terminate(new DateTime(2026, 1, 31));

        var act = () => employee.Terminate(new DateTime(2026, 2, 28));

        act.Should().Throw<InvalidOperationException>().WithMessage("*already inactive*");
    }

    [Theory]
    [InlineData("2024-01-15", "2024-01-15", 0)]
    [InlineData("2024-01-15", "2024-07-15", 6)]
    [InlineData("2023-01-15", "2026-01-15", 36)]
    public void TenureInMonths_counts_whole_months(string joined, string asOf, int expected)
    {
        var employee = NewEmployee(DateTime.Parse(joined));

        employee.TenureInMonths(DateTime.Parse(asOf)).Should().Be(expected);
    }

    [Fact]
    public void FullName_joins_first_and_last_name()
    {
        NewEmployee().FullName.Should().Be("Ayesha Khan");
    }
}
