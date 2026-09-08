namespace HRM.EmployeeService.Domain;

public enum Gender { Male = 1, Female = 2, Other = 3 }
public enum EmploymentType { FullTime = 1, PartTime = 2, Contract = 3, Intern = 4, Consultant = 5 }
public enum EmploymentStatus { Active = 1, Probation = 2, OnLeave = 3, Suspended = 4, Resigned = 5, Terminated = 6 }

/// <summary>
/// Employee aggregate root. Owned exclusively by Employee Service - no other
/// service touches this table. They react to events or call the REST API instead.
/// </summary>
public class Employee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EmployeeCode { get; set; } = string.Empty;   // EMP-2026-0001

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string? ProfilePhotoUrl { get; set; }

    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string? Address { get; set; }
    public string? Nationality { get; set; }

    // Ids referencing the Organization Service. Deliberately not FKs.
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }   // denormalised copy kept fresh by events
    public Guid? DesignationId { get; set; }
    public string? DesignationName { get; set; }
    public Guid? ManagerId { get; set; }

    public DateTime JoiningDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;
    public EmploymentStatus EmploymentStatus { get; set; } = EmploymentStatus.Active;

    public decimal BasicSalary { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }

    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<EmployeeDocument> Documents { get; set; } = [];

    // ---- Behaviour lives on the entity, not in the controller ----
    public void Terminate(DateTime terminationDate)
    {
        if (EmploymentStatus is EmploymentStatus.Terminated or EmploymentStatus.Resigned)
            throw new InvalidOperationException("Employee is already inactive.");
        if (terminationDate.Date < JoiningDate.Date)
            throw new InvalidOperationException("Termination date cannot be before the joining date.");

        EmploymentStatus = EmploymentStatus.Terminated;
        TerminationDate = terminationDate;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public int TenureInMonths(DateTime asOf) =>
        Math.Max(0, ((asOf.Year - JoiningDate.Year) * 12) + asOf.Month - JoiningDate.Month);
}

public class EmployeeDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? DocumentType { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}
