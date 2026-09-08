using HRM.IdentityService.Domain;
using HRM.IdentityService.Infrastructure;
using HRM.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace HRM.IdentityService.Application;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct);
    Task LogoutAsync(string refreshToken, CancellationToken ct);
    Task<UserDto> RegisterAsync(RegisterUserRequest request, CancellationToken ct);
    Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct);
}

public class AuthService(IdentityDbContext db, ITokenService tokens, ILogger<AuthService> logger) : IAuthService
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await LoadUserGraph().FirstOrDefaultAsync(u => u.Email == request.Email.Trim(), ct);

        // Same generic message for "no user" and "wrong password" - do not help attackers enumerate accounts.
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("This account is disabled.");

        user.LastLoginAtUtc = DateTime.UtcNow;
        var response = await IssueTokensAsync(user, ct);
        logger.LogInformation("User {Email} signed in", user.Email);
        return response;
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var hash = tokens.Hash(refreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
                     ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (stored.RevokedAtUtc is not null || stored.ExpiresAtUtc <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token expired or revoked.");

        var user = await LoadUserGraph().FirstAsync(u => u.Id == stored.UserId, ct);

        // Rotation: the old token dies the moment a new one is issued.
        stored.RevokedAtUtc = DateTime.UtcNow;
        var response = await IssueTokensAsync(user, ct, stored);
        return response;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct)
    {
        var hash = tokens.Hash(refreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is null) return;
        stored.RevokedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<UserDto> RegisterAsync(RegisterUserRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException($"A user with email {email} already exists.");

        var roles = await db.Roles.Where(r => request.Roles.Contains(r.Name)).ToListAsync(ct);
        if (roles.Count != request.Roles.Count)
            throw new DomainRuleException("One or more roles do not exist.");

        var user = new AppUser
        {
            Email = email,
            FullName = request.FullName.Trim(),
            EmployeeId = request.EmployeeId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };
        foreach (var role in roles)
            user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return await GetCurrentUserAsync(user.Id, ct);
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await LoadUserGraph().FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new NotFoundException("User not found.");
        return ToDto(user);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new NotFoundException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new DomainRuleException("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        // Changing a password kills every existing session.
        var active = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null).ToListAsync(ct);
        active.ForEach(t => t.RevokedAtUtc = DateTime.UtcNow);

        await db.SaveChangesAsync(ct);
    }

    private IQueryable<AppUser> LoadUserGraph() =>
        db.Users
          .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
              .ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
          .AsSplitQuery();

    private async Task<AuthResponse> IssueTokensAsync(AppUser user, CancellationToken ct, RefreshToken? replacing = null)
    {
        var dto = ToDto(user);
        var (accessToken, expiresAt) = tokens.CreateAccessToken(user, dto.Roles, dto.Permissions);
        var (rawRefresh, refreshHash, refreshExpiry) = tokens.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id, TokenHash = refreshHash, ExpiresAtUtc = refreshExpiry
        });
        if (replacing is not null) replacing.ReplacedByTokenHash = refreshHash;

        await db.SaveChangesAsync(ct);
        return new AuthResponse(accessToken, rawRefresh, expiresAt, dto);
    }

    private static UserDto ToDto(AppUser user) => new(
        user.Id, user.Email, user.FullName, user.EmployeeId, user.IsActive,
        user.UserRoles.Select(ur => ur.Role.Name).ToList(),
        user.UserRoles.SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Code))
                      .Distinct().ToList());
}
