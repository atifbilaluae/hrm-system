using HRM.NotificationService.Domain;
using Microsoft.EntityFrameworkCore;

namespace HRM.NotificationService.Infrastructure;

public class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Notification>(e =>
        {
            e.ToTable("Notifications");
            e.HasIndex(x => new { x.RecipientEmployeeId, x.IsRead });
            e.HasIndex(x => x.CreatedAtUtc);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Message).HasMaxLength(1000).IsRequired();
        });
    }
}
