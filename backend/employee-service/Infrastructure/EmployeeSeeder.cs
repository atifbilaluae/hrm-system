using HRM.EmployeeService.Domain;
using Microsoft.EntityFrameworkCore;

namespace HRM.EmployeeService.Infrastructure;

/// <summary>Demo data so the dashboard is not empty on first run. Disabled by Seed:Enabled=false.</summary>
public static class EmployeeSeeder
{
    public static async Task SeedAsync(EmployeeDbContext db, CancellationToken ct = default)
    {
        if (await db.Employees.AnyAsync(ct)) return;

        var departments = new (Guid Id, string Name, string[] Designations)[]
        {
            (Guid.Parse("11111111-1111-1111-1111-111111111111"), "Engineering", ["Software Engineer", "Senior Engineer", "Tech Lead"]),
            (Guid.Parse("22222222-2222-2222-2222-222222222222"), "Human Resources", ["HR Executive", "HR Manager"]),
            (Guid.Parse("33333333-3333-3333-3333-333333333333"), "Finance", ["Accountant", "Finance Manager"]),
            (Guid.Parse("44444444-4444-4444-4444-444444444444"), "Sales", ["Sales Executive", "Sales Manager"])
        };

        var firstNames = new[] { "Ahmed", "Sara", "Bilal", "Ayesha", "Usman", "Hina", "Fahad", "Zainab", "Omar", "Maryam", "Hassan", "Nida" };
        var lastNames = new[] { "Khan", "Ali", "Malik", "Sheikh", "Raza", "Iqbal", "Farooq", "Hussain" };
        var random = new Random(42);
        var employees = new List<Employee>();

        for (var i = 0; i < 24; i++)
        {
            var dept = departments[i % departments.Length];
            var first = firstNames[i % firstNames.Length];
            var last = lastNames[random.Next(lastNames.Length)];
            employees.Add(new Employee
            {
                EmployeeCode = $"EMP-2026-{i + 1:D4}",
                FirstName = first,
                LastName = last,
                Email = $"{first.ToLower()}.{last.ToLower()}{i}@hrm.local",
                Phone = $"+92-300-{random.Next(1000000, 9999999)}",
                DateOfBirth = new DateTime(1988 + (i % 12), (i % 12) + 1, ((i * 3) % 27) + 1),
                Gender = i % 3 == 0 ? Gender.Female : Gender.Male,
                Nationality = "Pakistani",
                Address = "Lahore, Pakistan",
                DepartmentId = dept.Id,
                DepartmentName = dept.Name,
                DesignationName = dept.Designations[i % dept.Designations.Length],
                JoiningDate = DateTime.UtcNow.AddMonths(-random.Next(1, 48)).Date,
                EmploymentType = EmploymentType.FullTime,
                EmploymentStatus = i % 7 == 0 ? EmploymentStatus.Probation : EmploymentStatus.Active,
                BasicSalary = 80_000 + (random.Next(0, 20) * 5_000),
                BankName = "Meezan Bank",
                EmergencyContactName = "Family Contact",
                EmergencyContactPhone = "+92-321-1234567",
                EmergencyContactRelation = "Sibling"
            });
        }

        db.Employees.AddRange(employees);
        await db.SaveChangesAsync(ct);
    }
}
