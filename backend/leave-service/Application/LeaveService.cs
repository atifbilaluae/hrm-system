using HRM.LeaveService.Domain;
using HRM.LeaveService.Infrastructure;
using HRM.Shared.Common;
using HRM.Shared.Contracts;
using HRM.Shared.Messaging;
using Microsoft.EntityFrameworkCore;

namespace HRM.LeaveService.Application;

public interface ILeaveService
{
    Task<List<LeaveTypeDto>> GetLeaveTypesAsync(CancellationToken ct);
    Task<LeaveTypeDto> CreateLeaveTypeAsync(SaveLeaveTypeRequest request, CancellationToken ct);
    Task<LeaveTypeDto> UpdateLeaveTypeAsync(Guid id, SaveLeaveTypeRequest request, CancellationToken ct);

    Task<List<LeaveBalanceDto>> GetBalancesAsync(Guid employeeId, int year, CancellationToken ct);
    Task EnsureBalancesAsync(Guid employeeId, int year, CancellationToken ct);

    Task<LeaveRequestDto> SubmitAsync(Guid employeeId, Guid? managerId, CreateLeaveRequest request, CancellationToken ct);
    Task<PagedResult<LeaveRequestDto>> GetRequestsAsync(Guid? employeeId, LeaveRequestStatus? status,
        int page, int pageSize, CancellationToken ct);
    Task<LeaveRequestDto> DecideAsync(Guid requestId, Guid? approverId, DecideLeaveRequest request, CancellationToken ct);
    Task<LeaveRequestDto> CancelAsync(Guid requestId, Guid employeeId, CancellationToken ct);
    Task<List<LeaveCalendarEntryDto>> GetCalendarAsync(DateTime from, DateTime to, CancellationToken ct);
}

public class LeaveAppService(
    LeaveDbContext db,
    IEventPublisher events,
    ILogger<LeaveAppService> logger) : ILeaveService
{
    // ---------------------------------------------------------------- leave types
    public async Task<List<LeaveTypeDto>> GetLeaveTypesAsync(CancellationToken ct) =>
        await db.LeaveTypes.AsNoTracking().OrderBy(t => t.Name)
            .Select(t => new LeaveTypeDto(t.Id, t.Name, t.Code, t.AnnualQuotaDays, t.IsPaid,
                t.RequiresApproval, t.MaxConsecutiveDays, t.MinimumNoticeDays, t.IsActive))
            .ToListAsync(ct);

    public async Task<LeaveTypeDto> CreateLeaveTypeAsync(SaveLeaveTypeRequest request, CancellationToken ct)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.LeaveTypes.AnyAsync(t => t.Code == code, ct))
            throw new ConflictException($"Leave type code {code} already exists.");

        var type = new LeaveType
        {
            Name = request.Name.Trim(), Code = code, AnnualQuotaDays = request.AnnualQuotaDays,
            IsPaid = request.IsPaid, RequiresApproval = request.RequiresApproval,
            MaxConsecutiveDays = request.MaxConsecutiveDays,
            MinimumNoticeDays = request.MinimumNoticeDays, IsActive = request.IsActive
        };
        db.LeaveTypes.Add(type);
        await db.SaveChangesAsync(ct);

        return (await GetLeaveTypesAsync(ct)).First(t => t.Id == type.Id);
    }

    public async Task<LeaveTypeDto> UpdateLeaveTypeAsync(Guid id, SaveLeaveTypeRequest request, CancellationToken ct)
    {
        var type = await db.LeaveTypes.FirstOrDefaultAsync(t => t.Id == id, ct)
                   ?? throw new NotFoundException($"Leave type {id} was not found.");

        type.Name = request.Name.Trim();
        type.AnnualQuotaDays = request.AnnualQuotaDays;
        type.IsPaid = request.IsPaid;
        type.RequiresApproval = request.RequiresApproval;
        type.MaxConsecutiveDays = request.MaxConsecutiveDays;
        type.MinimumNoticeDays = request.MinimumNoticeDays;
        type.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);

        return (await GetLeaveTypesAsync(ct)).First(t => t.Id == id);
    }

    // ---------------------------------------------------------------- balances
    public async Task<List<LeaveBalanceDto>> GetBalancesAsync(Guid employeeId, int year, CancellationToken ct)
    {
        await EnsureBalancesAsync(employeeId, year, ct);

        return await db.LeaveBalances.AsNoTracking().Include(b => b.LeaveType)
            .Where(b => b.EmployeeId == employeeId && b.Year == year)
            .OrderBy(b => b.LeaveType.Name)
            .Select(b => new LeaveBalanceDto(b.LeaveTypeId, b.LeaveType.Name, b.LeaveType.Code,
                b.Year, b.EntitledDays, b.UsedDays, b.PendingDays,
                b.EntitledDays - b.UsedDays - b.PendingDays))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Creates any missing balance rows for the year. Called on demand and also by the
    /// employee.created consumer, so a new hire has balances before they ever log in.
    /// </summary>
    public async Task EnsureBalancesAsync(Guid employeeId, int year, CancellationToken ct)
    {
        var activeTypes = await db.LeaveTypes.Where(t => t.IsActive).ToListAsync(ct);
        var existing = await db.LeaveBalances
            .Where(b => b.EmployeeId == employeeId && b.Year == year)
            .Select(b => b.LeaveTypeId).ToListAsync(ct);

        var missing = activeTypes.Where(t => !existing.Contains(t.Id))
            .Select(t => new LeaveBalance
            {
                EmployeeId = employeeId, LeaveTypeId = t.Id, Year = year,
                EntitledDays = t.AnnualQuotaDays
            }).ToList();

        if (missing.Count == 0) return;

        db.LeaveBalances.AddRange(missing);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Created {Count} leave balances for {EmployeeId} ({Year})",
            missing.Count, employeeId, year);
    }

    // ---------------------------------------------------------------- requests
    public async Task<LeaveRequestDto> SubmitAsync(Guid employeeId, Guid? managerId,
        CreateLeaveRequest request, CancellationToken ct)
    {
        var type = await db.LeaveTypes.FirstOrDefaultAsync(t => t.Id == request.LeaveTypeId && t.IsActive, ct)
                   ?? throw new NotFoundException("That leave type is not available.");

        var from = request.FromDate.Date;
        var to = request.ToDate.Date;

        // Rule 1: no two open requests covering the same dates.
        var clashing = await db.LeaveRequests
            .Where(r => r.EmployeeId == employeeId
                        && (r.Status == LeaveRequestStatus.Pending || r.Status == LeaveRequestStatus.Approved)
                        && r.FromDate <= to && from <= r.ToDate)
            .AnyAsync(ct);

        if (clashing)
            throw new ConflictException("You already have leave booked or pending across those dates.");

        // Rule 2: weekends do not consume the balance.
        var days = LeaveCalculator.CountWorkingDays(from, to);
        if (days <= 0)
            throw new DomainRuleException("That range contains no working days.");

        // Rule 3: respect the per-type consecutive limit.
        if (days > type.MaxConsecutiveDays)
            throw new DomainRuleException(
                $"{type.Name} allows at most {type.MaxConsecutiveDays} consecutive days.");

        // Rule 4: notice period.
        if (type.MinimumNoticeDays > 0 && from < DateTime.UtcNow.Date.AddDays(type.MinimumNoticeDays))
            throw new DomainRuleException(
                $"{type.Name} needs at least {type.MinimumNoticeDays} day(s) of notice.");

        await EnsureBalancesAsync(employeeId, from.Year, ct);
        var balance = await db.LeaveBalances.Include(b => b.LeaveType)
            .FirstAsync(b => b.EmployeeId == employeeId && b.LeaveTypeId == type.Id && b.Year == from.Year, ct);

        // Rule 5: hold the days now so a second request cannot spend the same balance.
        try { balance.Hold(days); }
        catch (InvalidOperationException ex) { throw new DomainRuleException(ex.Message); }

        var leaveRequest = new LeaveRequest
        {
            EmployeeId = employeeId,
            LeaveTypeId = type.Id,
            FromDate = from,
            ToDate = to,
            TotalDays = days,
            Reason = request.Reason.Trim(),
            ApproverEmployeeId = managerId,
            Status = type.RequiresApproval ? LeaveRequestStatus.Pending : LeaveRequestStatus.Approved
        };

        if (!type.RequiresApproval)
        {
            balance.Consume(days);
            leaveRequest.DecidedAtUtc = DateTime.UtcNow;
        }

        db.LeaveRequests.Add(leaveRequest);
        await db.SaveChangesAsync(ct);

        await events.PublishAsync(new LeaveRequestedEvent
        {
            LeaveRequestId = leaveRequest.Id,
            EmployeeId = employeeId,
            ApproverId = managerId,
            LeaveType = type.Name,
            FromDate = from,
            ToDate = to
        }, ct);

        logger.LogInformation("Leave request {Id} submitted for {Days} day(s)", leaveRequest.Id, days);
        return ToDto(leaveRequest, type.Name);
    }

    public async Task<PagedResult<LeaveRequestDto>> GetRequestsAsync(Guid? employeeId,
        LeaveRequestStatus? status, int page, int pageSize, CancellationToken ct)
    {
        var query = db.LeaveRequests.AsNoTracking().Include(r => r.LeaveType).AsQueryable();
        if (employeeId.HasValue) query = query.Where(r => r.EmployeeId == employeeId);
        if (status.HasValue) query = query.Where(r => r.Status == status);

        var total = await query.CountAsync(ct);
        var size = Math.Clamp(pageSize, 1, 100);
        var items = await query.OrderByDescending(r => r.CreatedAtUtc)
            .Skip((Math.Max(1, page) - 1) * size).Take(size).ToListAsync(ct);

        return new PagedResult<LeaveRequestDto>
        {
            Items = items.Select(r => ToDto(r, r.LeaveType.Name)).ToList(),
            Page = page, PageSize = size, TotalCount = total
        };
    }

    public async Task<LeaveRequestDto> DecideAsync(Guid requestId, Guid? approverId,
        DecideLeaveRequest request, CancellationToken ct)
    {
        var leaveRequest = await db.LeaveRequests.Include(r => r.LeaveType)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new NotFoundException($"Leave request {requestId} was not found.");

        try { leaveRequest.Decide(request.Approve, approverId, request.Comments); }
        catch (InvalidOperationException ex) { throw new DomainRuleException(ex.Message); }

        var balance = await db.LeaveBalances.FirstOrDefaultAsync(
            b => b.EmployeeId == leaveRequest.EmployeeId
                 && b.LeaveTypeId == leaveRequest.LeaveTypeId
                 && b.Year == leaveRequest.FromDate.Year, ct);

        // Approve turns the hold into usage; reject gives the days back.
        if (balance is not null)
        {
            if (request.Approve) balance.Consume(leaveRequest.TotalDays);
            else balance.Release(leaveRequest.TotalDays);
        }

        await db.SaveChangesAsync(ct);

        await events.PublishAsync(new LeaveDecisionEvent
        {
            LeaveRequestId = leaveRequest.Id,
            EmployeeId = leaveRequest.EmployeeId,
            Approved = request.Approve,
            Comments = request.Comments
        }, ct);

        return ToDto(leaveRequest, leaveRequest.LeaveType.Name);
    }

    public async Task<LeaveRequestDto> CancelAsync(Guid requestId, Guid employeeId, CancellationToken ct)
    {
        var leaveRequest = await db.LeaveRequests.Include(r => r.LeaveType)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new NotFoundException($"Leave request {requestId} was not found.");

        if (leaveRequest.EmployeeId != employeeId)
            throw new DomainRuleException("You can only cancel your own leave requests.");

        var wasApproved = leaveRequest.Status == LeaveRequestStatus.Approved;
        try { leaveRequest.Cancel(); }
        catch (InvalidOperationException ex) { throw new DomainRuleException(ex.Message); }

        var balance = await db.LeaveBalances.FirstOrDefaultAsync(
            b => b.EmployeeId == employeeId && b.LeaveTypeId == leaveRequest.LeaveTypeId
                 && b.Year == leaveRequest.FromDate.Year, ct);

        if (balance is not null)
        {
            if (wasApproved) balance.UsedDays = Math.Max(0, balance.UsedDays - leaveRequest.TotalDays);
            else balance.Release(leaveRequest.TotalDays);
        }

        await db.SaveChangesAsync(ct);
        return ToDto(leaveRequest, leaveRequest.LeaveType.Name);
    }

    public async Task<List<LeaveCalendarEntryDto>> GetCalendarAsync(DateTime from, DateTime to, CancellationToken ct) =>
        await db.LeaveRequests.AsNoTracking().Include(r => r.LeaveType)
            .Where(r => r.Status != LeaveRequestStatus.Rejected
                        && r.Status != LeaveRequestStatus.Cancelled
                        && r.FromDate <= to.Date && from.Date <= r.ToDate)
            .OrderBy(r => r.FromDate)
            .Select(r => new LeaveCalendarEntryDto(r.Id, r.EmployeeId, r.LeaveType.Name,
                r.FromDate, r.ToDate, r.Status))
            .ToListAsync(ct);

    private static LeaveRequestDto ToDto(LeaveRequest r, string leaveTypeName) => new(
        r.Id, r.EmployeeId, r.LeaveTypeId, leaveTypeName, r.FromDate, r.ToDate, r.TotalDays,
        r.Reason, r.Status, r.ApproverEmployeeId, r.DecisionComments, r.DecidedAtUtc, r.CreatedAtUtc);
}
