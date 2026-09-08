namespace HRM.OrganizationService.Domain;

/// <summary>
/// A department can have a parent, which is how the reporting hierarchy is stored:
/// a self-referencing tree instead of a separate hierarchy table.
/// </summary>
public class Department
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    public Department? ParentDepartment { get; set; }
    public ICollection<Department> ChildDepartments { get; set; } = [];
    /// <summary>Id from Employee Service. No FK - different database.</summary>
    public Guid? HeadEmployeeId { get; set; }
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<Designation> Designations { get; set; } = [];

    public void MoveUnder(Guid? parentId)
    {
        if (parentId == Id)
            throw new InvalidOperationException("A department cannot be its own parent.");
        ParentDepartmentId = parentId;
    }
}

public class Designation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    /// <summary>1 = entry level, higher numbers are more senior. Drives approval routing later.</summary>
    public int Level { get; set; } = 1;
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
}

public class Branch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Address { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = "Asia/Karachi";
    public bool IsHeadOffice { get; set; }
    public bool IsDeleted { get; set; }
    public ICollection<Department> Departments { get; set; } = [];
}

/// <summary>
/// The company calendar. Attendance and Leave both need it, so it lives with the
/// organization structure and is read through the API by those services.
/// </summary>
public class Holiday
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public bool IsRecurring { get; set; }
    public Guid? BranchId { get; set; }
    public string? Description { get; set; }
}
