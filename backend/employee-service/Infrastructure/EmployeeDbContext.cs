using HRM.EmployeeService.Domain;
using Microsoft.EntityFrameworkCore;

namespace HRM.EmployeeService.Infrastructure;

public class EmployeeDbContext(DbContextOptions<EmployeeDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeDocument> Documents => Set<EmployeeDocument>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Employee>(e =>
        {
            e.ToTable("Employees");
            e.HasIndex(x => x.EmployeeCode).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.DepartmentId);
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.EmployeeCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.BasicSalary).HasPrecision(18, 2);
            e.Ignore(x => x.FullName);
            // Soft delete: deleted rows disappear from every query automatically.
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        b.Entity<EmployeeDocument>(e =>
        {
            e.ToTable("EmployeeDocuments");
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.Employee).WithMany(x => x.Documents)
             .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
