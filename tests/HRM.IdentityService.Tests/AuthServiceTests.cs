using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using HRM.IdentityService.Application;
using HRM.IdentityService.Domain;
using HRM.IdentityService.Infrastructure;
using HRM.Shared.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace HRM.IdentityService.Tests;

public class AuthServiceTests
{
    private static readonly JwtSettings Jwt = new()
    {
        Secret = "unit_test_secret_key_that_is_long_enough_1234567890",
        Issuer = "hrm.identity",
        Audience = "hrm.clients",
        AccessTokenMinutes = 30,
        RefreshTokenDays = 7
    };

    private static (AuthService Service, IdentityDbContext Db) CreateSut()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"identity-{Guid.NewGuid()}")
            .Options;
        var db = new IdentityDbContext(options);

        var role = new Role { Name = Roles.HrAdmin };
        var permission = new Permission { Code = Permissions.EmployeeCreate };
        role.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id, Permission = permission, Role = role });

        var user = new AppUser
        {
            Email = "hr@hrm.local",
            FullName = "HR Admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Correct@12345")
        };
        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, Role = role, User = user });

        db.Permissions.Add(permission);
        db.Roles.Add(role);
        db.Users.Add(user);
        db.SaveChanges();

        var tokenService = new TokenService(Options.Create(Jwt));
        return (new AuthService(db, tokenService, NullLogger<AuthService>.Instance), db);
    }

    [Fact]
    public async Task Login_succeeds_with_valid_credentials()
    {
        var (sut, _) = CreateSut();

        var result = await sut.LoginAsync(new LoginRequest("hr@hrm.local", "Correct@12345"), CancellationToken.None);

        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.User.Roles.Should().Contain(Roles.HrAdmin);
    }

    [Fact]
    public async Task Login_fails_with_a_wrong_password()
    {
        var (sut, _) = CreateSut();

        var act = () => sut.LoginAsync(new LoginRequest("hr@hrm.local", "Wrong@12345"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Login_gives_the_same_error_for_an_unknown_email()
    {
        var (sut, _) = CreateSut();

        var act = () => sut.LoginAsync(new LoginRequest("nobody@hrm.local", "Correct@12345"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
                 .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task Access_token_carries_role_and_permission_claims()
    {
        var (sut, _) = CreateSut();

        var result = await sut.LoginAsync(new LoginRequest("hr@hrm.local", "Correct@12345"), CancellationToken.None);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);

        token.Claims.Should().Contain(c => c.Type == HrmClaimTypes.Permission && c.Value == Permissions.EmployeeCreate);
        token.Issuer.Should().Be(Jwt.Issuer);
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_revokes_the_old_one()
    {
        var (sut, db) = CreateSut();
        var login = await sut.LoginAsync(new LoginRequest("hr@hrm.local", "Correct@12345"), CancellationToken.None);

        var refreshed = await sut.RefreshAsync(login.RefreshToken, CancellationToken.None);

        refreshed.RefreshToken.Should().NotBe(login.RefreshToken);
        (await db.RefreshTokens.CountAsync(t => t.RevokedAtUtc != null)).Should().Be(1);
    }

    [Fact]
    public async Task A_revoked_refresh_token_cannot_be_reused()
    {
        var (sut, _) = CreateSut();
        var login = await sut.LoginAsync(new LoginRequest("hr@hrm.local", "Correct@12345"), CancellationToken.None);
        await sut.RefreshAsync(login.RefreshToken, CancellationToken.None);

        var act = () => sut.RefreshAsync(login.RefreshToken, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Changing_the_password_revokes_every_active_session()
    {
        var (sut, db) = CreateSut();
        var user = await db.Users.FirstAsync();
        await sut.LoginAsync(new LoginRequest("hr@hrm.local", "Correct@12345"), CancellationToken.None);

        await sut.ChangePasswordAsync(user.Id,
            new ChangePasswordRequest("Correct@12345", "Brand@New12345"), CancellationToken.None);

        (await db.RefreshTokens.CountAsync(t => t.RevokedAtUtc == null)).Should().Be(0);
    }
}
