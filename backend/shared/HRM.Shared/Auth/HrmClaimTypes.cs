namespace HRM.Shared.Auth;

public static class HrmClaimTypes
{
    public const string Permission = "permission";
    public const string EmployeeId = "employee_id";
    public const string FullName = "full_name";
}

/// <summary>Single source of truth for permission strings across all services.</summary>
public static class Permissions
{
    public const string EmployeeView = "employee.view";
    public const string EmployeeCreate = "employee.create";
    public const string EmployeeUpdate = "employee.update";
    public const string EmployeeDelete = "employee.delete";
    public const string AttendanceView = "attendance.view";
    public const string AttendanceManage = "attendance.manage";
    public const string LeaveRequest = "leave.request";
    public const string LeaveApprove = "leave.approve";
    public const string PayrollView = "payroll.view";
    public const string PayrollProcess = "payroll.process";
    public const string RecruitmentManage = "recruitment.manage";
    public const string PerformanceManage = "performance.manage";
    public const string UserManage = "user.manage";
    public const string OrganizationManage = "organization.manage";

    public static readonly string[] All =
    [
        EmployeeView, EmployeeCreate, EmployeeUpdate, EmployeeDelete,
        AttendanceView, AttendanceManage, LeaveRequest, LeaveApprove,
        PayrollView, PayrollProcess, RecruitmentManage, PerformanceManage,
        UserManage, OrganizationManage
    ];
}

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string HrAdmin = "HRAdmin";
    public const string HrManager = "HRManager";
    public const string DepartmentManager = "DepartmentManager";
    public const string PayrollAdmin = "PayrollAdmin";
    public const string Recruiter = "Recruiter";
    public const string Employee = "Employee";
}
