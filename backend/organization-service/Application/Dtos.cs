namespace HRM.OrganizationService.Application;

public record DepartmentDto(Guid Id, string Name, string Code, string? Description,
    Guid? ParentDepartmentId, string? ParentDepartmentName, Guid? HeadEmployeeId,
    Guid? BranchId, string? BranchName, bool IsActive, int DesignationCount);

public record DepartmentTreeNode(Guid Id, string Name, string Code, Guid? HeadEmployeeId,
    List<DepartmentTreeNode> Children);

public record SaveDepartmentRequest(string Name, string Code, string? Description,
    Guid? ParentDepartmentId, Guid? HeadEmployeeId, Guid? BranchId, bool IsActive = true);

public record DesignationDto(Guid Id, string Title, int Level, Guid? DepartmentId,
    string? DepartmentName, bool IsActive);

public record SaveDesignationRequest(string Title, int Level, Guid? DepartmentId, bool IsActive = true);

public record BranchDto(Guid Id, string Name, string? Code, string? Address, string City,
    string Country, string TimeZoneId, bool IsHeadOffice, int DepartmentCount);

public record SaveBranchRequest(string Name, string? Code, string? Address, string City,
    string Country, string TimeZoneId, bool IsHeadOffice);

public record HolidayDto(Guid Id, string Name, DateTime Date, bool IsRecurring, string? Description);

public record SaveHolidayRequest(string Name, DateTime Date, bool IsRecurring, Guid? BranchId, string? Description);
