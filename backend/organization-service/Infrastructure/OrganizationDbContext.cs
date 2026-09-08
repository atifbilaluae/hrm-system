using HRM.OrganizationService.Domain;
using Microsoft.EntityFrameworkCore;

namespace HRM.OrganizationService.Infrastructure;

public class OrganizationDbContext(DbContextOptions<OrganizationDbContext> options) : DbContext(options)
{
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Designation> Designations => Set<Designation>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Holiday> Holidays => Set<Holiday>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Department>(e =>
        {
            e.ToTable("Departments");
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Code).HasMaxLength(30).IsRequired();
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasOne(x => x.ParentDepartment).WithMany(x => x.ChildDepartments)
             .HasForeignKey(x => x.ParentDepartmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Branch).WithMany(x => x.Departments)
             .HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<Designation>(e =>
        {
            e.ToTable("Designations");
            e.Property(x => x.Title).HasMaxLength(150).IsRequired();
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasOne(x => x.Department).WithMany(x => x.Designations)
             .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<Branch>(e =>
        {
            e.ToTable("Branches");
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.City).HasMaxLength(100).IsRequired();
            e.Property(x => x.Country).HasMaxLength(100).IsRequired();
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        b.Entity<Holiday>(e =>
        {
            e.ToTable("Holidays");
            e.HasIndex(x => x.Date);
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
        });
    }
}
