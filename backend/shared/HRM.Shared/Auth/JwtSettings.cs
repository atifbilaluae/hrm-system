namespace HRM.Shared.Auth;

/// <summary>Strongly-typed JWT config. Values come from env vars, never hardcoded.</summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "hrm.identity";
    public string Audience { get; set; } = "hrm.clients";
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 7;
}
