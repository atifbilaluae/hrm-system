using HRM.IdentityService.Domain;
using HRM.Shared.Auth;
using Microsoft.EntityFrameworkCore;

namespace HRM.IdentityService.Infrastructure;

/// <summary>Seeds roles, permissions and a default super admin so the system is usable on first run.</summary>
public static class DbSeeder
{
    private static readonly Dictionary<string, string[]> RoleMatrix = new()
    {
        [Roles.SuperAdmin] = Permissions.All,
        [Roles.HrAdmin] =
        [
            Permissions.EmployeeView, Permissions.EmployeeCreate, Permissions.EmployeeUpdate, Permissions.EmployeeDelete,
            Permissions.AttendanceView, Permissions.AttendanceManage, Permissions.LeaveApprove, Permissions.LeaveRequest,
            Permissions.OrganizationManage, Permissions.PerformanceManage, Permissions.RecruitmentManage, Permissions.UserManage
        ],
        [Roles.HrManager] =
        [
            Permissions.EmployeeView, Permissions.EmployeeCreate, Permissions.EmployeeUpdate,
            Permissions.AttendanceView, Permissions.LeaveApprove, Permissions.LeaveRequest, Permissions.PerformanceManage
        ],
        [Roles.DepartmentManager] =
        [
            Permissions.EmployeeView, Permissions.AttendanceView, Permissions.LeaveApprove,
            Permissions.LeaveRequest, Permissions.PerformanceManage
        ],
        [Roles.PayrollAdmin] = [Permissions.EmployeeView, Permissions.PayrollView, Permissions.PayrollProcess],
        [Roles.Recruiter]    = [Permissions.EmployeeView, Permissions.RecruitmentManage],
        [Roles.Employee]     = [Permissions.LeaveRequest, Permissions.AttendanceView]
    };

    public static async Task SeedAsync(IdentityDbContext db, string adminEmail, string adminPassword, CancellationToken ct = default)
    {
        // Permissions
        var existingCodes = await db.Permissions.Select(p => p.Code).ToListAsync(ct);
        var missing = Permissions.All.Except(existingCodes)
            .Select(code => new Permission { Code = code, Description = code });
        db.Permissions.AddRange(missing);
        await db.SaveChangesAsync(ct);

        var allPermissions = await db.Permissions.ToDictionaryAsync(p => p.Code, ct);

        // Roles + role-permission matrix
        foreach (var (roleName, permissionCodes) in RoleMatrix)
        {
            var role = await db.Roles.Include(r => r.RolePermissions)
                                     .FirstOrDefaultAsync(r => r.Name == roleName, ct);
            if (role is null)
            {
                role = new Role { Name = roleName, Description = roleName };
                db.Roles.Add(role);
                await db.SaveChangesAsync(ct);
            }

            foreach (var code in permissionCodes)
            {
                var permissionId = allPermissions[code].Id;
                if (role.RolePermissions.Any(rp => rp.PermissionId == permissionId)) continue;
                db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });
            }
        }
        await db.SaveChangesAsync(ct);

        // Default super admin
        if (!await db.Users.AnyAsync(u => u.Email == adminEmail, ct))
        {
            var superAdminRole = await db.Roles.FirstAsync(r => r.Name == Roles.SuperAdmin, ct);
            var admin = new AppUser
            {
                Email = adminEmail,
                FullName = "System Administrator",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword)
            };
            admin.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = superAdminRole.Id });
            db.Users.Add(admin);
            await db.SaveChangesAsync(ct);
        }
    }
}
