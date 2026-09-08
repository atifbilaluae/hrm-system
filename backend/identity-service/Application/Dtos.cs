namespace HRM.IdentityService.Application;

public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record RegisterUserRequest(
    string Email,
    string Password,
    string FullName,
    Guid? EmployeeId,
    List<string> Roles);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAtUtc,
    UserDto User);

public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    Guid? EmployeeId,
    bool IsActive,
    List<string> Roles,
    List<string> Permissions);
