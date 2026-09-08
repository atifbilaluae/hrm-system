using HRM.AttendanceService.Domain;
using Microsoft.EntityFrameworkCore;

namespace HRM.AttendanceService.Infrastructure;

public class AttendanceDbContext(DbContextOptions<AttendanceDbContext> options) : DbContext(options)
{
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<AttendanceCorrectionRequest> CorrectionRequests => Set<AttendanceCorrectionRequest>();
    public DbSet<AttendancePolicy> Policies => Set<AttendancePolicy>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AttendanceRecord>(e =>
        {
            e.ToTable("AttendanceRecords");
            // One record per employee per day - enforced by the database, not by hope.
            e.HasIndex(x => new { x.EmployeeId, x.WorkDate }).IsUnique();
            e.HasIndex(x => x.WorkDate);
            e.Property(x => x.Notes).HasMaxLength(500);
        });

        b.Entity<AttendanceCorrectionRequest>(e =>
        {
            e.ToTable("AttendanceCorrectionRequests");
            e.HasIndex(x => new { x.EmployeeId, x.Status });
            e.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            e.Property(x => x.ReviewComments).HasMaxLength(500);
        });

        b.Entity<AttendancePolicy>(e =>
        {
            e.ToTable("AttendancePolicies");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
        });
    }
}
