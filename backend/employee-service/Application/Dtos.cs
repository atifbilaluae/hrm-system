using HRM.EmployeeService.Domain;

namespace HRM.EmployeeService.Application;

public record EmployeeListItemDto(
    Guid Id, string EmployeeCode, string FullName, string Email, string? Phone,
    string? DepartmentName, string? DesignationName, EmploymentStatus EmploymentStatus,
    DateTime JoiningDate, string? ProfilePhotoUrl);

public record EmployeeDetailDto(
    Guid Id, string EmployeeCode, string FirstName, string LastName, string Email, string? Phone,
    DateTime DateOfBirth, Gender Gender, string? Address, string? Nationality,
    Guid? DepartmentId, string? DepartmentName, Guid? DesignationId, string? DesignationName,
    Guid? ManagerId, DateTime JoiningDate, DateTime? TerminationDate,
    EmploymentType EmploymentType, EmploymentStatus EmploymentStatus,
    decimal BasicSalary, string? BankName, string? BankAccountNumber,
    string? EmergencyContactName, string? EmergencyContactPhone, string? EmergencyContactRelation,
    string? ProfilePhotoUrl, DateTime CreatedAtUtc);

public record CreateEmployeeRequest(
    string FirstName, string LastName, string Email, string? Phone,
    DateTime DateOfBirth, Gender Gender, string? Address, string? Nationality,
    Guid? DepartmentId, string? DepartmentName, Guid? DesignationId, string? DesignationName,
    Guid? ManagerId, DateTime JoiningDate, EmploymentType EmploymentType,
    decimal BasicSalary, string? BankName, string? BankAccountNumber,
    string? EmergencyContactName, string? EmergencyContactPhone, string? EmergencyContactRelation);

public record UpdateEmployeeRequest(
    string FirstName, string LastName, string? Phone, DateTime DateOfBirth, Gender Gender,
    string? Address, string? Nationality, Guid? DepartmentId, string? DepartmentName,
    Guid? DesignationId, string? DesignationName, Guid? ManagerId,
    EmploymentType EmploymentType, EmploymentStatus EmploymentStatus, decimal BasicSalary,
    string? BankName, string? BankAccountNumber,
    string? EmergencyContactName, string? EmergencyContactPhone, string? EmergencyContactRelation);

public record TerminateEmployeeRequest(DateTime TerminationDate, string? Reason);

public record EmployeeQuery(
    string? Search = null, Guid? DepartmentId = null, EmploymentStatus? Status = null,
    int Page = 1, int PageSize = 10, string SortBy = "joiningDate", bool Desc = true);

public record EmployeeStatsDto(
    int TotalEmployees, int ActiveEmployees, int NewThisMonth, int OnLeave,
    int Probation, List<DepartmentHeadcountDto> HeadcountByDepartment,
    List<UpcomingBirthdayDto> UpcomingBirthdays);

public record DepartmentHeadcountDto(string Department, int Count);
public record UpcomingBirthdayDto(Guid EmployeeId, string FullName, DateTime DateOfBirth, int DaysAway);
