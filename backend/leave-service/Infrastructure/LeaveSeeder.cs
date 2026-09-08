using HRM.LeaveService.Domain;
using Microsoft.EntityFrameworkCore;

namespace HRM.LeaveService.Infrastructure;

public static class LeaveSeeder
{
    public static async Task SeedAsync(LeaveDbContext db, CancellationToken ct = default)
    {
        if (await db.LeaveTypes.AnyAsync(ct)) return;

        db.LeaveTypes.AddRange(
            new LeaveType { Name = "Annual Leave", Code = "ANNUAL", AnnualQuotaDays = 20,
                            MaxConsecutiveDays = 15, MinimumNoticeDays = 3, CarryForward = true },
            new LeaveType { Name = "Sick Leave", Code = "SICK", AnnualQuotaDays = 10,
                            MaxConsecutiveDays = 7 },
            new LeaveType { Name = "Casual Leave", Code = "CASUAL", AnnualQuotaDays = 10,
                            MaxConsecutiveDays = 3 },
            new LeaveType { Name = "Maternity Leave", Code = "MATERNITY", AnnualQuotaDays = 90,
                            MaxConsecutiveDays = 90, MinimumNoticeDays = 30 },
            new LeaveType { Name = "Unpaid Leave", Code = "UNPAID", AnnualQuotaDays = 30,
                            IsPaid = false, MaxConsecutiveDays = 30 });

        await db.SaveChangesAsync(ct);
    }
}
