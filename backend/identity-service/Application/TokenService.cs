using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HRM.IdentityService.Domain;
using HRM.Shared.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HRM.IdentityService.Application;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateAccessToken(AppUser user, IEnumerable<string> roles, IEnumerable<string> permissions);
    (string RawToken, string TokenHash, DateTime ExpiresAtUtc) CreateRefreshToken();
    string Hash(string rawToken);
}

public class TokenService(IOptions<JwtSettings> options) : ITokenService
{
    private readonly JwtSettings _jwt = options.Value;

    public (string Token, DateTime ExpiresAtUtc) CreateAccessToken(
        AppUser user, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var expires = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(HrmClaimTypes.FullName, user.FullName)
        };

        if (user.EmployeeId.HasValue)
            claims.Add(new Claim(HrmClaimTypes.EmployeeId, user.EmployeeId.Value.ToString()));

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        // Permissions are baked into the token so downstream services authorize
        // locally without calling Identity Service on every request.
        claims.AddRange(permissions.Distinct().Select(p => new Claim(HrmClaimTypes.Permission, p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public (string RawToken, string TokenHash, DateTime ExpiresAtUtc) CreateRefreshToken()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return (raw, Hash(raw), DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays));
    }

    public string Hash(string rawToken) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
