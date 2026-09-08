using HRM.OrganizationService.Domain;
using HRM.OrganizationService.Infrastructure;
using HRM.Shared.Common;
using HRM.Shared.Contracts;
using HRM.Shared.Messaging;
using Microsoft.EntityFrameworkCore;

namespace HRM.OrganizationService.Application;

public interface IOrganizationService
{
    Task<List<DepartmentDto>> GetDepartmentsAsync(CancellationToken ct);
    Task<List<DepartmentTreeNode>> GetDepartmentTreeAsync(CancellationToken ct);
    Task<DepartmentDto> CreateDepartmentAsync(SaveDepartmentRequest request, CancellationToken ct);
    Task<DepartmentDto> UpdateDepartmentAsync(Guid id, SaveDepartmentRequest request, CancellationToken ct);
    Task DeleteDepartmentAsync(Guid id, CancellationToken ct);

    Task<List<DesignationDto>> GetDesignationsAsync(Guid? departmentId, CancellationToken ct);
    Task<DesignationDto> CreateDesignationAsync(SaveDesignationRequest request, CancellationToken ct);
    Task<DesignationDto> UpdateDesignationAsync(Guid id, SaveDesignationRequest request, CancellationToken ct);
    Task DeleteDesignationAsync(Guid id, CancellationToken ct);

    Task<List<BranchDto>> GetBranchesAsync(CancellationToken ct);
    Task<BranchDto> CreateBranchAsync(SaveBranchRequest request, CancellationToken ct);
    Task<BranchDto> UpdateBranchAsync(Guid id, SaveBranchRequest request, CancellationToken ct);

    Task<List<HolidayDto>> GetHolidaysAsync(int year, CancellationToken ct);
    Task<HolidayDto> CreateHolidayAsync(SaveHolidayRequest request, CancellationToken ct);
    Task DeleteHolidayAsync(Guid id, CancellationToken ct);
}

public class OrganizationAppService(
    OrganizationDbContext db,
    IEventPublisher events,
    ILogger<OrganizationAppService> logger) : IOrganizationService
{
    // ---------------------------------------------------------------- departments
    public async Task<List<DepartmentDto>> GetDepartmentsAsync(CancellationToken ct) =>
        await db.Departments.AsNoTracking()
            .Include(d => d.ParentDepartment).Include(d => d.Branch)
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(d.Id, d.Name, d.Code, d.Description,
                d.ParentDepartmentId, d.ParentDepartment != null ? d.ParentDepartment.Name : null,
                d.HeadEmployeeId, d.BranchId, d.Branch != null ? d.Branch.Name : null,
                d.IsActive, d.Designations.Count(x => !x.IsDeleted)))
            .ToListAsync(ct);

    public async Task<List<DepartmentTreeNode>> GetDepartmentTreeAsync(CancellationToken ct)
    {
        // Load the flat list once and build the tree in memory. Recursive SQL for a
        // few dozen departments would cost more than it saves.
        var all = await db.Departments.AsNoTracking()
            .Select(d => new { d.Id, d.Name, d.Code, d.HeadEmployeeId, d.ParentDepartmentId })
            .ToListAsync(ct);

        var byParent = all.ToLookup(d => d.ParentDepartmentId);

        List<DepartmentTreeNode> Build(Guid? parentId) =>
            byParent[parentId]
                .OrderBy(d => d.Name)
                .Select(d => new DepartmentTreeNode(d.Id, d.Name, d.Code, d.HeadEmployeeId, Build(d.Id)))
                .ToList();

        return Build(null);
    }

    public async Task<DepartmentDto> CreateDepartmentAsync(SaveDepartmentRequest request, CancellationToken ct)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.Departments.IgnoreQueryFilters().AnyAsync(d => d.Code == code, ct))
            throw new ConflictException($"Department code {code} is already in use.");

        var department = new Department
        {
            Name = request.Name.Trim(),
            Code = code,
            Description = request.Description,
            ParentDepartmentId = request.ParentDepartmentId,
            HeadEmployeeId = request.HeadEmployeeId,
            BranchId = request.BranchId,
            IsActive = request.IsActive
        };

        db.Departments.Add(department);
        await db.SaveChangesAsync(ct);
        await PublishDepartmentChangedAsync(department, ct);

        logger.LogInformation("Created department {Code}", department.Code);
        return await SingleDepartmentAsync(department.Id, ct);
    }

    public async Task<DepartmentDto> UpdateDepartmentAsync(Guid id, SaveDepartmentRequest request, CancellationToken ct)
    {
        var department = await db.Departments.FirstOrDefaultAsync(d => d.Id == id, ct)
                         ?? throw new NotFoundException($"Department {id} was not found.");

        if (await WouldCreateCycleAsync(id, request.ParentDepartmentId, ct))
            throw new DomainRuleException("That parent would create a loop in the hierarchy.");

        department.Name = request.Name.Trim();
        department.Description = request.Description;
        department.HeadEmployeeId = request.HeadEmployeeId;
        department.BranchId = request.BranchId;
        department.IsActive = request.IsActive;

        try { department.MoveUnder(request.ParentDepartmentId); }
        catch (InvalidOperationException ex) { throw new DomainRuleException(ex.Message); }

        await db.SaveChangesAsync(ct);

        // Employee Service keeps a denormalised copy of the department name.
        // This event is what keeps that copy honest.
        await PublishDepartmentChangedAsync(department, ct);
        return await SingleDepartmentAsync(department.Id, ct);
    }

    public async Task DeleteDepartmentAsync(Guid id, CancellationToken ct)
    {
        var department = await db.Departments.Include(d => d.ChildDepartments)
                                             .FirstOrDefaultAsync(d => d.Id == id, ct)
                         ?? throw new NotFoundException($"Department {id} was not found.");

        if (department.ChildDepartments.Any(c => !c.IsDeleted))
            throw new DomainRuleException("Move or remove the sub-departments first.");

        department.IsDeleted = true;
        await db.SaveChangesAsync(ct);
        await PublishDepartmentChangedAsync(department, ct, deleted: true);
    }

    // ---------------------------------------------------------------- designations
    public async Task<List<DesignationDto>> GetDesignationsAsync(Guid? departmentId, CancellationToken ct)
    {
        var query = db.Designations.AsNoTracking().Include(d => d.Department).AsQueryable();
        if (departmentId.HasValue) query = query.Where(d => d.DepartmentId == departmentId);

        return await query.OrderByDescending(d => d.Level).ThenBy(d => d.Title)
            .Select(d => new DesignationDto(d.Id, d.Title, d.Level, d.DepartmentId,
                d.Department != null ? d.Department.Name : null, d.IsActive))
            .ToListAsync(ct);
    }

    public async Task<DesignationDto> CreateDesignationAsync(SaveDesignationRequest request, CancellationToken ct)
    {
        var designation = new Designation
        {
            Title = request.Title.Trim(),
            Level = request.Level,
            DepartmentId = request.DepartmentId,
            IsActive = request.IsActive
        };
        db.Designations.Add(designation);
        await db.SaveChangesAsync(ct);
        return (await GetDesignationsAsync(null, ct)).First(d => d.Id == designation.Id);
    }

    public async Task<DesignationDto> UpdateDesignationAsync(Guid id, SaveDesignationRequest request, CancellationToken ct)
    {
        var designation = await db.Designations.FirstOrDefaultAsync(d => d.Id == id, ct)
                          ?? throw new NotFoundException($"Designation {id} was not found.");

        designation.Title = request.Title.Trim();
        designation.Level = request.Level;
        designation.DepartmentId = request.DepartmentId;
        designation.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);

        return (await GetDesignationsAsync(null, ct)).First(d => d.Id == id);
    }

    public async Task DeleteDesignationAsync(Guid id, CancellationToken ct)
    {
        var designation = await db.Designations.FirstOrDefaultAsync(d => d.Id == id, ct)
                          ?? throw new NotFoundException($"Designation {id} was not found.");
        designation.IsDeleted = true;
        await db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------- branches
    public async Task<List<BranchDto>> GetBranchesAsync(CancellationToken ct) =>
        await db.Branches.AsNoTracking()
            .OrderByDescending(b => b.IsHeadOffice).ThenBy(b => b.Name)
            .Select(b => new BranchDto(b.Id, b.Name, b.Code, b.Address, b.City, b.Country,
                b.TimeZoneId, b.IsHeadOffice, b.Departments.Count(d => !d.IsDeleted)))
            .ToListAsync(ct);

    public async Task<BranchDto> CreateBranchAsync(SaveBranchRequest request, CancellationToken ct)
    {
        var branch = new Branch
        {
            Name = request.Name.Trim(), Code = request.Code, Address = request.Address,
            City = request.City.Trim(), Country = request.Country.Trim(),
            TimeZoneId = request.TimeZoneId, IsHeadOffice = request.IsHeadOffice
        };
        db.Branches.Add(branch);
        await db.SaveChangesAsync(ct);
        return (await GetBranchesAsync(ct)).First(b => b.Id == branch.Id);
    }

    public async Task<BranchDto> UpdateBranchAsync(Guid id, SaveBranchRequest request, CancellationToken ct)
    {
        var branch = await db.Branches.FirstOrDefaultAsync(b => b.Id == id, ct)
                     ?? throw new NotFoundException($"Branch {id} was not found.");

        branch.Name = request.Name.Trim();
        branch.Code = request.Code;
        branch.Address = request.Address;
        branch.City = request.City.Trim();
        branch.Country = request.Country.Trim();
        branch.TimeZoneId = request.TimeZoneId;
        branch.IsHeadOffice = request.IsHeadOffice;
        await db.SaveChangesAsync(ct);

        return (await GetBranchesAsync(ct)).First(b => b.Id == id);
    }

    // ---------------------------------------------------------------- holidays
    public async Task<List<HolidayDto>> GetHolidaysAsync(int year, CancellationToken ct) =>
        await db.Holidays.AsNoTracking()
            .Where(h => h.Date.Year == year)
            .OrderBy(h => h.Date)
            .Select(h => new HolidayDto(h.Id, h.Name, h.Date, h.IsRecurring, h.Description))
            .ToListAsync(ct);

    public async Task<HolidayDto> CreateHolidayAsync(SaveHolidayRequest request, CancellationToken ct)
    {
        var holiday = new Holiday
        {
            Name = request.Name.Trim(), Date = request.Date.Date,
            IsRecurring = request.IsRecurring, BranchId = request.BranchId,
            Description = request.Description
        };
        db.Holidays.Add(holiday);
        await db.SaveChangesAsync(ct);
        return new HolidayDto(holiday.Id, holiday.Name, holiday.Date, holiday.IsRecurring, holiday.Description);
    }

    public async Task DeleteHolidayAsync(Guid id, CancellationToken ct)
    {
        var holiday = await db.Holidays.FirstOrDefaultAsync(h => h.Id == id, ct)
                      ?? throw new NotFoundException($"Holiday {id} was not found.");
        db.Holidays.Remove(holiday);
        await db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------- helpers
    private async Task<DepartmentDto> SingleDepartmentAsync(Guid id, CancellationToken ct) =>
        (await GetDepartmentsAsync(ct)).First(d => d.Id == id);

    private async Task<bool> WouldCreateCycleAsync(Guid departmentId, Guid? newParentId, CancellationToken ct)
    {
        // Walk up from the proposed parent. If we meet ourselves, it is a cycle.
        var cursor = newParentId;
        var guard = 0;
        while (cursor.HasValue && guard++ < 50)
        {
            if (cursor == departmentId) return true;
            cursor = await db.Departments.Where(d => d.Id == cursor)
                             .Select(d => d.ParentDepartmentId).FirstOrDefaultAsync(ct);
        }
        return false;
    }

    private Task PublishDepartmentChangedAsync(Department department, CancellationToken ct, bool deleted = false) =>
        events.PublishAsync(new DepartmentUpdatedEvent
        {
            DepartmentId = department.Id,
            Name = department.Name,
            Code = department.Code,
            IsDeleted = deleted
        }, ct);
}
