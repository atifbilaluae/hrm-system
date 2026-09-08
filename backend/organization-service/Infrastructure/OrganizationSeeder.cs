using HRM.OrganizationService.Domain;
using Microsoft.EntityFrameworkCore;

namespace HRM.OrganizationService.Infrastructure;

public static class OrganizationSeeder
{
    public static async Task SeedAsync(OrganizationDbContext db, CancellationToken ct = default)
    {
        if (await db.Departments.AnyAsync(ct)) return;

        var head = new Branch
        {
            Name = "Head Office", Code = "HO", City = "Lahore", Country = "Pakistan",
            Address = "Gulberg III", IsHeadOffice = true
        };
        var karachi = new Branch { Name = "Karachi Office", Code = "KHI", City = "Karachi", Country = "Pakistan" };
        db.Branches.AddRange(head, karachi);

        // Ids match the demo employees seeded by Employee Service so the two line up.
        var departments = new[]
        {
            new Department { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Engineering", Code = "ENG", BranchId = head.Id },
            new Department { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Human Resources", Code = "HR", BranchId = head.Id },
            new Department { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Finance", Code = "FIN", BranchId = head.Id },
            new Department { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Sales", Code = "SALES", BranchId = karachi.Id }
        };
        db.Departments.AddRange(departments);

        db.Designations.AddRange(
            new Designation { Title = "Software Engineer", Level = 2, DepartmentId = departments[0].Id },
            new Designation { Title = "Senior Engineer", Level = 3, DepartmentId = departments[0].Id },
            new Designation { Title = "Tech Lead", Level = 4, DepartmentId = departments[0].Id },
            new Designation { Title = "HR Executive", Level = 2, DepartmentId = departments[1].Id },
            new Designation { Title = "HR Manager", Level = 4, DepartmentId = departments[1].Id },
            new Designation { Title = "Accountant", Level = 2, DepartmentId = departments[2].Id },
            new Designation { Title = "Finance Manager", Level = 4, DepartmentId = departments[2].Id },
            new Designation { Title = "Sales Executive", Level = 2, DepartmentId = departments[3].Id },
            new Designation { Title = "Sales Manager", Level = 4, DepartmentId = departments[3].Id });

        var year = DateTime.UtcNow.Year;
        db.Holidays.AddRange(
            new Holiday { Name = "Kashmir Day", Date = new DateTime(year, 2, 5), IsRecurring = true },
            new Holiday { Name = "Pakistan Day", Date = new DateTime(year, 3, 23), IsRecurring = true },
            new Holiday { Name = "Labour Day", Date = new DateTime(year, 5, 1), IsRecurring = true },
            new Holiday { Name = "Independence Day", Date = new DateTime(year, 8, 14), IsRecurring = true },
            new Holiday { Name = "Iqbal Day", Date = new DateTime(year, 11, 9), IsRecurring = true },
            new Holiday { Name = "Quaid-e-Azam Day", Date = new DateTime(year, 12, 25), IsRecurring = true });

        await db.SaveChangesAsync(ct);
    }
}
