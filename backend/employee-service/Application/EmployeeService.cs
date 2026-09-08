using HRM.EmployeeService.Domain;
using HRM.EmployeeService.Infrastructure;
using HRM.Shared.Common;
using HRM.Shared.Contracts;
using HRM.Shared.Messaging;
using Microsoft.EntityFrameworkCore;

namespace HRM.EmployeeService.Application;

public interface IEmployeeService
{
    Task<PagedResult<EmployeeListItemDto>> SearchAsync(EmployeeQuery query, CancellationToken ct);
    Task<EmployeeDetailDto> GetByIdAsync(Guid id, CancellationToken ct);
    Task<EmployeeDetailDto> CreateAsync(CreateEmployeeRequest request, CancellationToken ct);
    Task<EmployeeDetailDto> UpdateAsync(Guid id, UpdateEmployeeRequest request, CancellationToken ct);
    Task TerminateAsync(Guid id, TerminateEmployeeRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<EmployeeStatsDto> GetStatsAsync(CancellationToken ct);
}

public class EmployeeAppService(
    EmployeeDbContext db,
    ICacheService cache,
    IEventPublisher events,
    IHttpContextAccessor http,
    ILogger<EmployeeAppService> logger) : IEmployeeService
{
    private static readonly TimeSpan DetailTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan StatsTtl = TimeSpan.FromMinutes(5);

    public async Task<PagedResult<EmployeeListItemDto>> SearchAsync(EmployeeQuery query, CancellationToken ct)
    {
        // Lists are NOT cached: they are filtered a hundred different ways and change on every write.
        var q = db.Employees.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(e => e.FirstName.Contains(term) || e.LastName.Contains(term)
                          || e.Email.Contains(term) || e.EmployeeCode.Contains(term));
        }
        if (query.DepartmentId.HasValue) q = q.Where(e => e.DepartmentId == query.DepartmentId);
        if (query.Status.HasValue) q = q.Where(e => e.EmploymentStatus == query.Status);

        q = (query.SortBy.ToLowerInvariant(), query.Desc) switch
        {
            ("name", true) => q.OrderByDescending(e => e.FirstName),
            ("name", false) => q.OrderBy(e => e.FirstName),
            ("code", true) => q.OrderByDescending(e => e.EmployeeCode),
            ("code", false) => q.OrderBy(e => e.EmployeeCode),
            (_, false) => q.OrderBy(e => e.JoiningDate),
            _ => q.OrderByDescending(e => e.JoiningDate)
        };

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<EmployeeListItemDto>
        {
            Items = items.Select(e => e.ToListItem()).ToList(),
            Page = page, PageSize = pageSize, TotalCount = total
        };
    }

    public async Task<EmployeeDetailDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var cached = await cache.GetAsync<EmployeeDetailDto>(CacheKeys.Employee(id), ct);
        if (cached is not null) return cached;

        var employee = await db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct)
                       ?? throw new NotFoundException($"Employee {id} was not found.");

        var dto = employee.ToDetail();
        await cache.SetAsync(CacheKeys.Employee(id), dto, DetailTtl, ct);
        return dto;
    }

    public async Task<EmployeeDetailDto> CreateAsync(CreateEmployeeRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Employees.IgnoreQueryFilters().AnyAsync(e => e.Email == email, ct))
            throw new ConflictException($"An employee with email {email} already exists.");

        var employee = new Employee
        {
            EmployeeCode = await GenerateEmployeeCodeAsync(ct),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            Phone = request.Phone,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Address = request.Address,
            Nationality = request.Nationality,
            DepartmentId = request.DepartmentId,
            DepartmentName = request.DepartmentName,
            DesignationId = request.DesignationId,
            DesignationName = request.DesignationName,
            ManagerId = request.ManagerId,
            JoiningDate = request.JoiningDate,
            EmploymentType = request.EmploymentType,
            EmploymentStatus = EmploymentStatus.Probation,
            BasicSalary = request.BasicSalary,
            BankName = request.BankName,
            BankAccountNumber = request.BankAccountNumber,
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactPhone = request.EmergencyContactPhone,
            EmergencyContactRelation = request.EmergencyContactRelation
        };

        db.Employees.Add(employee);
        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKeys.Stats, ct);

        // Fire-and-forget to the rest of the platform: Leave service creates balances,
        // Payroll sets up a salary structure, Notification tells the manager.
        await events.PublishAsync(new EmployeeCreatedEvent
        {
            EmployeeId = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            FullName = employee.FullName,
            Email = employee.Email,
            DepartmentId = employee.DepartmentId,
            ManagerId = employee.ManagerId,
            JoiningDate = employee.JoiningDate,
            CorrelationId = CorrelationId()
        }, ct);

        logger.LogInformation("Created employee {Code}", employee.EmployeeCode);
        return employee.ToDetail();
    }

    public async Task<EmployeeDetailDto> UpdateAsync(Guid id, UpdateEmployeeRequest request, CancellationToken ct)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == id, ct)
                       ?? throw new NotFoundException($"Employee {id} was not found.");

        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.Phone = request.Phone;
        employee.DateOfBirth = request.DateOfBirth;
        employee.Gender = request.Gender;
        employee.Address = request.Address;
        employee.Nationality = request.Nationality;
        employee.DepartmentId = request.DepartmentId;
        employee.DepartmentName = request.DepartmentName;
        employee.DesignationId = request.DesignationId;
        employee.DesignationName = request.DesignationName;
        employee.ManagerId = request.ManagerId;
        employee.EmploymentType = request.EmploymentType;
        employee.EmploymentStatus = request.EmploymentStatus;
        employee.BasicSalary = request.BasicSalary;
        employee.BankName = request.BankName;
        employee.BankAccountNumber = request.BankAccountNumber;
        employee.EmergencyContactName = request.EmergencyContactName;
        employee.EmergencyContactPhone = request.EmergencyContactPhone;
        employee.EmergencyContactRelation = request.EmergencyContactRelation;
        employee.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await InvalidateAsync(id, ct);

        await events.PublishAsync(new EmployeeUpdatedEvent
        {
            EmployeeId = employee.Id, FullName = employee.FullName,
            Email = employee.Email, DepartmentId = employee.DepartmentId,
            CorrelationId = CorrelationId()
        }, ct);

        return employee.ToDetail();
    }

    public async Task TerminateAsync(Guid id, TerminateEmployeeRequest request, CancellationToken ct)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == id, ct)
                       ?? throw new NotFoundException($"Employee {id} was not found.");
        try
        {
            employee.Terminate(request.TerminationDate);
        }
        catch (InvalidOperationException ex)
        {
            throw new DomainRuleException(ex.Message);
        }

        await db.SaveChangesAsync(ct);
        await InvalidateAsync(id, ct);

        await events.PublishAsync(new EmployeeTerminatedEvent
        {
            EmployeeId = employee.Id, FullName = employee.FullName,
            TerminationDate = request.TerminationDate, CorrelationId = CorrelationId()
        }, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.Id == id, ct)
                       ?? throw new NotFoundException($"Employee {id} was not found.");
        employee.IsDeleted = true;   // HR records are never hard-deleted (audit + legal retention)
        employee.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await InvalidateAsync(id, ct);
    }

    public async Task<EmployeeStatsDto> GetStatsAsync(CancellationToken ct)
    {
        var cached = await cache.GetAsync<EmployeeStatsDto>(CacheKeys.Stats, ct);
        if (cached is not null) return cached;

        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var employees = await db.Employees.AsNoTracking()
            .Select(e => new { e.Id, e.FirstName, e.LastName, e.DateOfBirth, e.DepartmentName, e.EmploymentStatus, e.JoiningDate })
            .ToListAsync(ct);

        var birthdays = employees
            .Select(e => new
            {
                e.Id, Name = $"{e.FirstName} {e.LastName}", e.DateOfBirth,
                Next = NextBirthday(e.DateOfBirth, today)
            })
            .Where(x => (x.Next - today).TotalDays <= 30)
            .OrderBy(x => x.Next)
            .Take(10)
            .Select(x => new UpcomingBirthdayDto(x.Id, x.Name, x.DateOfBirth, (int)(x.Next - today).TotalDays))
            .ToList();

        var stats = new EmployeeStatsDto(
            TotalEmployees: employees.Count,
            ActiveEmployees: employees.Count(e => e.EmploymentStatus is EmploymentStatus.Active or EmploymentStatus.Probation),
            NewThisMonth: employees.Count(e => e.JoiningDate >= monthStart),
            OnLeave: employees.Count(e => e.EmploymentStatus == EmploymentStatus.OnLeave),
            Probation: employees.Count(e => e.EmploymentStatus == EmploymentStatus.Probation),
            HeadcountByDepartment: employees
                .GroupBy(e => e.DepartmentName ?? "Unassigned")
                .Select(g => new DepartmentHeadcountDto(g.Key, g.Count()))
                .OrderByDescending(x => x.Count).ToList(),
            UpcomingBirthdays: birthdays);

        await cache.SetAsync(CacheKeys.Stats, stats, StatsTtl, ct);
        return stats;
    }

    private static DateTime NextBirthday(DateTime dob, DateTime today)
    {
        var next = new DateTime(today.Year, dob.Month, Math.Min(dob.Day, DateTime.DaysInMonth(today.Year, dob.Month)));
        return next < today ? next.AddYears(1) : next;
    }

    private async Task InvalidateAsync(Guid id, CancellationToken ct)
    {
        await cache.RemoveAsync(CacheKeys.Employee(id), ct);
        await cache.RemoveAsync(CacheKeys.Stats, ct);
    }

    private string? CorrelationId() =>
        http.HttpContext?.Items["X-Correlation-Id"]?.ToString();

    /// <summary>Sequential per-year code, e.g. EMP-2026-0042.</summary>
    private async Task<string> GenerateEmployeeCodeAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"EMP-{year}-";
        var count = await db.Employees.IgnoreQueryFilters()
            .CountAsync(e => e.EmployeeCode.StartsWith(prefix), ct);
        return $"{prefix}{count + 1:D4}";
    }
}
