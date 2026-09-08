using HRM.NotificationService.Domain;
using HRM.NotificationService.Infrastructure;
using HRM.Shared.Auth;
using HRM.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRM.NotificationService.Controllers;

public record NotificationDto(Guid Id, string Title, string Message, NotificationType Type,
    string? Link, bool IsRead, DateTime CreatedAtUtc);

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(NotificationDbContext db, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Notifications addressed to the signed-in employee, plus broadcasts.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<NotificationDto>>> GetMine(
        [FromQuery] bool unreadOnly = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var employeeId = currentUser.EmployeeId;
        var query = db.Notifications.AsNoTracking()
            .Where(n => n.RecipientEmployeeId == null || n.RecipientEmployeeId == employeeId);

        if (unreadOnly) query = query.Where(n => !n.IsRead);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(n => n.CreatedAtUtc)
            .Skip((Math.Max(1, page) - 1) * pageSize).Take(Math.Clamp(pageSize, 1, 100))
            .Select(n => new NotificationDto(n.Id, n.Title, n.Message, n.Type, n.Link, n.IsRead, n.CreatedAtUtc))
            .ToListAsync(ct);

        return Ok(new PagedResult<NotificationDto>
            { Items = items, Page = page, PageSize = pageSize, TotalCount = total });
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> UnreadCount(CancellationToken ct)
    {
        var employeeId = currentUser.EmployeeId;
        return Ok(await db.Notifications.CountAsync(
            n => !n.IsRead && (n.RecipientEmployeeId == null || n.RecipientEmployeeId == employeeId), ct));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var notification = await db.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct)
                           ?? throw new NotFoundException("Notification not found.");
        notification.IsRead = true;
        notification.ReadAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        var employeeId = currentUser.EmployeeId;
        await db.Notifications
            .Where(n => !n.IsRead && (n.RecipientEmployeeId == null || n.RecipientEmployeeId == employeeId))
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true)
                                      .SetProperty(n => n.ReadAtUtc, DateTime.UtcNow), ct);
        return NoContent();
    }
}
