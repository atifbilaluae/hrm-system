using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace HRM.Shared.Auth;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    Guid? EmployeeId { get; }
    IReadOnlyList<string> Roles { get; }
    bool HasPermission(string permission);
}

public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public Guid? UserId =>
        Guid.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? Email => User?.FindFirstValue(ClaimTypes.Email);

    public Guid? EmployeeId =>
        Guid.TryParse(User?.FindFirstValue(HrmClaimTypes.EmployeeId), out var id) ? id : null;

    public IReadOnlyList<string> Roles =>
        User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? [];

    public bool HasPermission(string permission) =>
        User?.Claims.Any(c => c.Type == HrmClaimTypes.Permission && c.Value == permission) ?? false;
}
