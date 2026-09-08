using HRM.IdentityService.Application;
using HRM.IdentityService.Infrastructure;
using HRM.Shared.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRM.IdentityService.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(IAuthService auth, IdentityDbContext db) : ControllerBase
{
    /// <summary>Create a user account. Usually called right after an employee is onboarded.</summary>
    [HttpPost]
    [HasPermission(Permissions.UserManage)]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<UserDto>> Create(RegisterUserRequest request, CancellationToken ct)
    {
        var user = await auth.RegisterAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.UserManage)]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await auth.GetCurrentUserAsync(id, ct));

    [HttpGet]
    [HasPermission(Permissions.UserManage)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll(CancellationToken ct)
    {
        // Project to an anonymous type in SQL, then build the DTO in memory.
        // EF cannot translate "new List<string>()" inside a projection.
        var rows = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .Select(u => new
            {
                u.Id, u.Email, u.FullName, u.EmployeeId, u.IsActive,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
            })
            .ToListAsync(ct);

        var users = rows
            .Select(u => new UserDto(u.Id, u.Email, u.FullName, u.EmployeeId, u.IsActive, u.Roles, []))
            .ToList();

        return Ok(users);
    }

    [HttpGet("roles")]
    [HasPermission(Permissions.UserManage)]
    public async Task<ActionResult<IEnumerable<object>>> GetRoles(CancellationToken ct)
        => Ok(await db.Roles.AsNoTracking()
            .Select(r => new { r.Id, r.Name, r.Description })
            .ToListAsync(ct));
}
