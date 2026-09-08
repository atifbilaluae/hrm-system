using FluentAssertions;
using HRM.EmployeeService.Application;
using HRM.EmployeeService.Domain;
using HRM.EmployeeService.Infrastructure;
using HRM.Shared.Common;
using HRM.Shared.Contracts;
using HRM.Shared.Messaging;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace HRM.EmployeeService.Tests;

public class EmployeeServiceTests
{
    private readonly Mock<IEventPublisher> _publisher = new();
    private readonly Mock<ICacheService> _cache = new();

    private EmployeeAppService CreateSut(out EmployeeDbContext db)
    {
        var options = new DbContextOptionsBuilder<EmployeeDbContext>()
            .UseInMemoryDatabase($"employees-{Guid.NewGuid()}")
            .Options;
        db = new EmployeeDbContext(options);

        _cache.Setup(c => c.GetAsync<EmployeeDetailDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((EmployeeDetailDto?)null);

        return new EmployeeAppService(db, _cache.Object, _publisher.Object,
            new HttpContextAccessor(), NullLogger<EmployeeAppService>.Instance);
    }

    private static CreateEmployeeRequest ValidRequest(string email = "new.hire@hrm.local") => new(
        "New", "Hire", email, "+92-300-1112222", new DateTime(1995, 4, 12), Gender.Female,
        "Lahore", "Pakistani", null, "Engineering", null, "Software Engineer", null,
        new DateTime(2026, 2, 1), EmploymentType.FullTime, 150_000, "Meezan", "0123456789",
        "Contact", "+92-321-9998888", "Sibling");

    [Fact]
    public async Task CreateAsync_generates_a_sequential_employee_code()
    {
        var sut = CreateSut(out _);

        var first = await sut.CreateAsync(ValidRequest("a@hrm.local"), CancellationToken.None);
        var second = await sut.CreateAsync(ValidRequest("b@hrm.local"), CancellationToken.None);

        first.EmployeeCode.Should().Be($"EMP-{DateTime.UtcNow.Year}-0001");
        second.EmployeeCode.Should().Be($"EMP-{DateTime.UtcNow.Year}-0002");
    }

    [Fact]
    public async Task CreateAsync_starts_new_hires_on_probation()
    {
        var sut = CreateSut(out _);

        var created = await sut.CreateAsync(ValidRequest(), CancellationToken.None);

        created.EmploymentStatus.Should().Be(EmploymentStatus.Probation);
    }

    [Fact]
    public async Task CreateAsync_publishes_an_EmployeeCreated_event()
    {
        var sut = CreateSut(out _);

        await sut.CreateAsync(ValidRequest(), CancellationToken.None);

        _publisher.Verify(p => p.PublishAsync(
            It.Is<EmployeeCreatedEvent>(e => e.Email == "new.hire@hrm.local"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_rejects_a_duplicate_email()
    {
        var sut = CreateSut(out _);
        await sut.CreateAsync(ValidRequest("dup@hrm.local"), CancellationToken.None);

        var act = () => sut.CreateAsync(ValidRequest("dup@hrm.local"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task GetByIdAsync_throws_when_the_employee_does_not_exist()
    {
        var sut = CreateSut(out _);

        var act = () => sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_soft_deletes_and_hides_the_record()
    {
        var sut = CreateSut(out var db);
        var created = await sut.CreateAsync(ValidRequest(), CancellationToken.None);

        await sut.DeleteAsync(created.Id, CancellationToken.None);

        (await db.Employees.CountAsync()).Should().Be(0);
        (await db.Employees.IgnoreQueryFilters().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SearchAsync_filters_by_name_and_pages_results()
    {
        var sut = CreateSut(out _);
        await sut.CreateAsync(ValidRequest("zara@hrm.local") with { FirstName = "Zara" }, CancellationToken.None);
        await sut.CreateAsync(ValidRequest("bilal@hrm.local") with { FirstName = "Bilal" }, CancellationToken.None);

        var result = await sut.SearchAsync(new EmployeeQuery(Search: "Zara", PageSize: 10), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Single().FullName.Should().StartWith("Zara");
    }

    [Fact]
    public async Task TerminateAsync_converts_a_domain_error_into_a_DomainRuleException()
    {
        var sut = CreateSut(out _);
        var created = await sut.CreateAsync(ValidRequest(), CancellationToken.None);

        var act = () => sut.TerminateAsync(created.Id,
            new TerminateEmployeeRequest(new DateTime(2020, 1, 1), "Too early"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainRuleException>();
    }
}
