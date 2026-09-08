using HRM.LeaveService.Domain;
using Microsoft.EntityFrameworkCore;

namespace HRM.LeaveService.Infrastructure;

public class LeaveDbContext(DbContextOptions<LeaveDbContext> options) : DbContext(options)
{
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<LeaveType>(e =>
        {
            e.ToTable("LeaveTypes");
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
        });

        b.Entity<LeaveBalance>(e =>
        {
            e.ToTable("LeaveBalances");
            e.HasIndex(x => new { x.EmployeeId, x.LeaveTypeId, x.Year }).IsUnique();
            e.Property(x => x.EntitledDays).HasPrecision(6, 2);
            e.Property(x => x.UsedDays).HasPrecision(6, 2);
            e.Property(x => x.PendingDays).HasPrecision(6, 2);
            e.Ignore(x => x.AvailableDays);
            e.HasOne(x => x.LeaveType).WithMany().HasForeignKey(x => x.LeaveTypeId);
        });

        b.Entity<LeaveRequest>(e =>
        {
            e.ToTable("LeaveRequests");
            e.HasIndex(x => new { x.EmployeeId, x.Status });
            e.HasIndex(x => new { x.FromDate, x.ToDate });
            e.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            e.Property(x => x.DecisionComments).HasMaxLength(500);
            e.Property(x => x.TotalDays).HasPrecision(6, 2);
            e.HasOne(x => x.LeaveType).WithMany().HasForeignKey(x => x.LeaveTypeId);
        });
    }
}
