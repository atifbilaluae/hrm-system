using HRM.EmployeeService.Domain;

namespace HRM.EmployeeService.Application;

public static class EmployeeMapper
{
    public static EmployeeListItemDto ToListItem(this Employee e) => new(
        e.Id, e.EmployeeCode, $"{e.FirstName} {e.LastName}", e.Email, e.Phone,
        e.DepartmentName, e.DesignationName, e.EmploymentStatus, e.JoiningDate, e.ProfilePhotoUrl);

    public static EmployeeDetailDto ToDetail(this Employee e) => new(
        e.Id, e.EmployeeCode, e.FirstName, e.LastName, e.Email, e.Phone, e.DateOfBirth, e.Gender,
        e.Address, e.Nationality, e.DepartmentId, e.DepartmentName, e.DesignationId, e.DesignationName,
        e.ManagerId, e.JoiningDate, e.TerminationDate, e.EmploymentType, e.EmploymentStatus,
        e.BasicSalary, e.BankName, e.BankAccountNumber, e.EmergencyContactName,
        e.EmergencyContactPhone, e.EmergencyContactRelation, e.ProfilePhotoUrl, e.CreatedAtUtc);
}
