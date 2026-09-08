namespace HRM.NotificationService.Domain;

public enum NotificationChannel { InApp = 1, Email = 2 }
public enum NotificationType { Info = 1, Success = 2, Warning = 3, Approval = 4 }

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Null = broadcast to everyone with the matching permission.</summary>
    public Guid? RecipientEmployeeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
    public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;
    public string? Link { get; set; }
    public string? SourceEvent { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAtUtc { get; set; }
}
