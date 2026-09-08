using HRM.AttendanceService.Domain;
using HRM.AttendanceService.Infrastructure;
using HRM.Shared.Common;
using HRM.Shared.Contracts;
using HRM.Shared.Messaging;
using Microsoft.EntityFrameworkCore;

namespace HRM.AttendanceService.Application;

public interface IAttendanceService
{
    Task<TodayAttendanceDto> GetTodayAsync(Guid employeeId, CancellationToken ct);
    Task<AttendanceRecordDto> CheckInAsync(Guid employeeId, CancellationToken ct);
    Task<AttendanceRecordDto> CheckOutAsync(Guid employeeId, CancellationToken ct);
    Task<PagedResult<AttendanceRecordDto>> GetDailyAsync(DateTime date, int page, int pageSize, CancellationToken ct);
    Task<List<AttendanceRecordDto>> GetForEmployeeAsync(Guid employeeId, int year, int month, CancellationToken ct);
    Task<AttendanceSummaryDto> GetMonthlySummaryAsync(Guid employeeId, int year, int month, CancellationToken ct);
    Task<DailyAttendanceSummaryDto> GetDailySummaryAsync(DateTime date, CancellationToken ct);
    Task<AttendanceRecordDto> SaveManualAsync(ManualAttendanceRequest request, CancellationToken ct);

    Task<CorrectionRequestDto> RequestCorrectionAsync(Guid employeeId, CreateCorrectionRequest request, CancellationToken ct);
    Task<List<CorrectionRequestDto>> GetCorrectionsAsync(Guid? employeeId, CorrectionStatus? status, CancellationToken ct);
    Task<CorrectionRequestDto> DecideCorrectionAsync(Guid id, Guid? reviewerId, DecideCorrectionRequest request, CancellationToken ct);
}

public class AttendanceAppService(
    AttendanceDbContext db,
    IEventPublisher events,
    ILogger<AttendanceAppService> logger) : IAttendanceService
{
    public async Task<TodayAttendanceDto> GetTodayAsync(Guid employeeId, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var record = await db.AttendanceRecords.AsNoTracking()
            .FirstOrDefaultAsync(r => r.EmployeeId == employeeId && r.WorkDate == today, ct);

        return new TodayAttendanceDto(
            record?.CheckInUtc is not null,
            record?.CheckOutUtc is not null,
            record is null ? null : ToDto(record));
    }

    public async Task<AttendanceRecordDto> CheckInAsync(Guid employeeId, CancellationToken ct)
    {
        var policy = await GetPolicyAsync(ct);
        var today = DateTime.UtcNow.Date;

        var record = await db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == employeeId && r.WorkDate == today, ct);

        if (record is null)
        {
            record = new AttendanceRecord { EmployeeId = employeeId, WorkDate = today };
            db.AttendanceRecords.Add(record);
        }

        Apply(() => record.CheckIn(DateTime.UtcNow, policy));
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Employee {EmployeeId} checked in ({Status})", employeeId, record.Status);
        return ToDto(record);
    }

    public async Task<AttendanceRecordDto> CheckOutAsync(Guid employeeId, CancellationToken ct)
    {
        var policy = await GetPolicyAsync(ct);
        var today = DateTime.UtcNow.Date;

        var record = await db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == employeeId && r.WorkDate == today, ct)
            ?? throw new DomainRuleException("There is no check-in recorded for today.");

        Apply(() => record.CheckOut(DateTime.UtcNow, policy));
        await db.SaveChangesAsync(ct);

        // Payroll needs overtime; the dashboard needs the daily count. Both subscribe to this.
        await events.PublishAsync(new AttendanceRecordedEvent
        {
            AttendanceId = record.Id,
            EmployeeId = record.EmployeeId,
            WorkDate = record.WorkDate,
            Status = record.Status.ToString(),
            WorkedMinutes = record.WorkedMinutes,
            OvertimeMinutes = record.OvertimeMinutes
        }, ct);

        return ToDto(record);
    }

    public async Task<PagedResult<AttendanceRecordDto>> GetDailyAsync(DateTime date, int page, int pageSize, CancellationToken ct)
    {
        var day = date.Date;
        var query = db.AttendanceRecords.AsNoTracking().Where(r => r.WorkDate == day);

        var total = await query.CountAsync(ct);
        var size = Math.Clamp(pageSize, 1, 100);
        var items = await query
            .OrderBy(r => r.CheckInUtc)
            .Skip((Math.Max(1, page) - 1) * size).Take(size)
            .ToListAsync(ct);

        return new PagedResult<AttendanceRecordDto>
        {
            Items = items.Select(ToDto).ToList(),
            Page = page, PageSize = size, TotalCount = total
        };
    }

    public async Task<List<AttendanceRecordDto>> GetForEmployeeAsync(Guid employeeId, int year, int month, CancellationToken ct)
    {
        var (from, to) = MonthRange(year, month);
        var records = await db.AttendanceRecords.AsNoTracking()
            .Where(r => r.EmployeeId == employeeId && r.WorkDate >= from && r.WorkDate < to)
            .OrderBy(r => r.WorkDate)
            .ToListAsync(ct);

        return records.Select(ToDto).ToList();
    }

    public async Task<AttendanceSummaryDto> GetMonthlySummaryAsync(Guid employeeId, int year, int month, CancellationToken ct)
    {
        var (from, to) = MonthRange(year, month);
        var records = await db.AttendanceRecords.AsNoTracking()
            .Where(r => r.EmployeeId == employeeId && r.WorkDate >= from && r.WorkDate < to)
            .ToListAsync(ct);

        return new AttendanceSummaryDto(year, month, employeeId,
            PresentDays: records.Count(r => r.Status == AttendanceStatus.Present),
            LateDays: records.Count(r => r.Status == AttendanceStatus.Late),
            HalfDays: records.Count(r => r.Status == AttendanceStatus.HalfDay),
            AbsentDays: records.Count(r => r.Status == AttendanceStatus.Absent),
            TotalWorkedMinutes: records.Sum(r => r.WorkedMinutes),
            TotalOvertimeMinutes: records.Sum(r => r.OvertimeMinutes));
    }

    public async Task<DailyAttendanceSummaryDto> GetDailySummaryAsync(DateTime date, CancellationToken ct)
    {
        var day = date.Date;
        var records = await db.AttendanceRecords.AsNoTracking()
            .Where(r => r.WorkDate == day).ToListAsync(ct);

        return new DailyAttendanceSummaryDto(day,
            Present: records.Count(r => r.Status == AttendanceStatus.Present),
            Late: records.Count(r => r.Status == AttendanceStatus.Late),
            HalfDay: records.Count(r => r.Status == AttendanceStatus.HalfDay),
            Absent: records.Count(r => r.Status == AttendanceStatus.Absent),
            TotalRecords: records.Count);
    }

    public async Task<AttendanceRecordDto> SaveManualAsync(ManualAttendanceRequest request, CancellationToken ct)
    {
        var policy = await GetPolicyAsync(ct);
        var day = request.WorkDate.Date;

        var record = await db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == request.EmployeeId && r.WorkDate == day, ct);

        if (record is null)
        {
            record = new AttendanceRecord { EmployeeId = request.EmployeeId, WorkDate = day };
            db.AttendanceRecords.Add(record);
        }

        Apply(() => record.ApplyCorrection(request.CheckInUtc, request.CheckOutUtc, policy));
        record.Notes = request.Notes;
        await db.SaveChangesAsync(ct);

        return ToDto(record);
    }

    // ---------------------------------------------------------------- corrections
    public async Task<CorrectionRequestDto> RequestCorrectionAsync(Guid employeeId, CreateCorrectionRequest request, CancellationToken ct)
    {
        var day = request.WorkDate.Date;
        var alreadyPending = await db.CorrectionRequests.AnyAsync(
            c => c.EmployeeId == employeeId && c.WorkDate == day && c.Status == CorrectionStatus.Pending, ct);

        if (alreadyPending)
            throw new ConflictException("You already have a pending correction for that date.");

        var correction = new AttendanceCorrectionRequest
        {
            EmployeeId = employeeId,
            WorkDate = day,
            RequestedCheckInUtc = request.RequestedCheckInUtc,
            RequestedCheckOutUtc = request.RequestedCheckOutUtc,
            Reason = request.Reason.Trim()
        };

        db.CorrectionRequests.Add(correction);
        await db.SaveChangesAsync(ct);
        return ToDto(correction);
    }

    public async Task<List<CorrectionRequestDto>> GetCorrectionsAsync(Guid? employeeId, CorrectionStatus? status, CancellationToken ct)
    {
        var query = db.CorrectionRequests.AsNoTracking().AsQueryable();
        if (employeeId.HasValue) query = query.Where(c => c.EmployeeId == employeeId);
        if (status.HasValue) query = query.Where(c => c.Status == status);

        var items = await query.OrderByDescending(c => c.CreatedAtUtc).Take(200).ToListAsync(ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<CorrectionRequestDto> DecideCorrectionAsync(Guid id, Guid? reviewerId, DecideCorrectionRequest request, CancellationToken ct)
    {
        var correction = await db.CorrectionRequests.FirstOrDefaultAsync(c => c.Id == id, ct)
                         ?? throw new NotFoundException($"Correction request {id} was not found.");

        Apply(() => correction.Decide(request.Approve, reviewerId, request.Comments));

        // Approving rewrites the attendance row using the requested times.
        if (request.Approve)
        {
            await SaveManualAsync(new ManualAttendanceRequest(
                correction.EmployeeId, correction.WorkDate,
                correction.RequestedCheckInUtc, correction.RequestedCheckOutUtc,
                $"Corrected: {correction.Reason}"), ct);
        }

        await db.SaveChangesAsync(ct);
        return ToDto(correction);
    }

    // ---------------------------------------------------------------- helpers
    private async Task<AttendancePolicy> GetPolicyAsync(CancellationToken ct) =>
        await db.Policies.AsNoTracking().FirstOrDefaultAsync(p => p.IsDefault, ct)
        ?? new AttendancePolicy();

    /// <summary>Domain methods throw InvalidOperationException; the API speaks 422.</summary>
    private static void Apply(Action action)
    {
        try { action(); }
        catch (InvalidOperationException ex) { throw new DomainRuleException(ex.Message); }
    }

    private static (DateTime From, DateTime To) MonthRange(int year, int month)
    {
        var from = new DateTime(year, month, 1);
        return (from, from.AddMonths(1));
    }

    private static AttendanceRecordDto ToDto(AttendanceRecord r) => new(
        r.Id, r.EmployeeId, r.WorkDate, r.CheckInUtc, r.CheckOutUtc, r.Status,
        r.WorkedMinutes, r.LateMinutes, r.EarlyDepartureMinutes, r.OvertimeMinutes,
        r.IsManualEntry, r.Notes);

    private static CorrectionRequestDto ToDto(AttendanceCorrectionRequest c) => new(
        c.Id, c.EmployeeId, c.WorkDate, c.RequestedCheckInUtc, c.RequestedCheckOutUtc,
        c.Reason, c.Status, c.ReviewComments, c.CreatedAtUtc);
}
