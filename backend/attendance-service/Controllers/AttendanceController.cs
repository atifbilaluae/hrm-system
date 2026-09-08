using HRM.AttendanceService.Application;
using HRM.AttendanceService.Domain;
using HRM.Shared.Auth;
using HRM.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.AttendanceService.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController(IAttendanceService attendance, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>The signed-in employee's own record for today. Drives the check-in widget.</summary>
    [HttpGet("today")]
    public async Task<ActionResult<TodayAttendanceDto>> Today(CancellationToken ct)
        => Ok(await attendance.GetTodayAsync(RequireEmployeeId(), ct));

    [HttpPost("check-in")]
    [ProducesResponseType<AttendanceRecordDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AttendanceRecordDto>> CheckIn(CancellationToken ct)
        => Ok(await attendance.CheckInAsync(RequireEmployeeId(), ct));

    [HttpPost("check-out")]
    public async Task<ActionResult<AttendanceRecordDto>> CheckOut(CancellationToken ct)
        => Ok(await attendance.CheckOutAsync(RequireEmployeeId(), ct));

    /// <summary>Everyone's attendance for one day. HR view.</summary>
    [HttpGet]
    [HasPermission(Permissions.AttendanceView)]
    public async Task<ActionResult<PagedResult<AttendanceRecordDto>>> Daily(
        [FromQuery] DateTime? date, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await attendance.GetDailyAsync(date ?? DateTime.UtcNow.Date, page, pageSize, ct));

    [HttpGet("summary/daily")]
    [HasPermission(Permissions.AttendanceView)]
    public async Task<ActionResult<DailyAttendanceSummaryDto>> DailySummary(
        [FromQuery] DateTime? date, CancellationToken ct)
        => Ok(await attendance.GetDailySummaryAsync(date ?? DateTime.UtcNow.Date, ct));

    [HttpGet("employee/{employeeId:guid}")]
    [HasPermission(Permissions.AttendanceView)]
    public async Task<ActionResult<List<AttendanceRecordDto>>> ForEmployee(
        Guid employeeId, [FromQuery] int? year, [FromQuery] int? month, CancellationToken ct)
        => Ok(await attendance.GetForEmployeeAsync(employeeId,
            year ?? DateTime.UtcNow.Year, month ?? DateTime.UtcNow.Month, ct));

    /// <summary>My own month. No extra permission needed - it is the caller's data.</summary>
    [HttpGet("me/monthly")]
    public async Task<ActionResult<List<AttendanceRecordDto>>> MyMonth(
        [FromQuery] int? year, [FromQuery] int? month, CancellationToken ct)
        => Ok(await attendance.GetForEmployeeAsync(RequireEmployeeId(),
            year ?? DateTime.UtcNow.Year, month ?? DateTime.UtcNow.Month, ct));

    [HttpGet("me/summary")]
    public async Task<ActionResult<AttendanceSummaryDto>> MySummary(
        [FromQuery] int? year, [FromQuery] int? month, CancellationToken ct)
        => Ok(await attendance.GetMonthlySummaryAsync(RequireEmployeeId(),
            year ?? DateTime.UtcNow.Year, month ?? DateTime.UtcNow.Month, ct));

    /// <summary>HR entering attendance on someone's behalf (missed punch, field work).</summary>
    [HttpPost("manual")]
    [HasPermission(Permissions.AttendanceManage)]
    public async Task<ActionResult<AttendanceRecordDto>> Manual(ManualAttendanceRequest request, CancellationToken ct)
        => Ok(await attendance.SaveManualAsync(request, ct));

    private Guid RequireEmployeeId() =>
        currentUser.EmployeeId
        ?? throw new DomainRuleException("This account is not linked to an employee record yet.");
}

[ApiController]
[Route("api/attendance/corrections")]
[Authorize]
public class AttendanceCorrectionsController(IAttendanceService attendance, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CorrectionRequestDto>> Create(CreateCorrectionRequest request, CancellationToken ct)
        => Ok(await attendance.RequestCorrectionAsync(RequireEmployeeId(), request, ct));

    [HttpGet("me")]
    public async Task<ActionResult<List<CorrectionRequestDto>>> Mine(CancellationToken ct)
        => Ok(await attendance.GetCorrectionsAsync(RequireEmployeeId(), null, ct));

    [HttpGet("pending")]
    [HasPermission(Permissions.AttendanceManage)]
    public async Task<ActionResult<List<CorrectionRequestDto>>> Pending(CancellationToken ct)
        => Ok(await attendance.GetCorrectionsAsync(null, CorrectionStatus.Pending, ct));

    [HttpPost("{id:guid}/decide")]
    [HasPermission(Permissions.AttendanceManage)]
    public async Task<ActionResult<CorrectionRequestDto>> Decide(Guid id, DecideCorrectionRequest request, CancellationToken ct)
        => Ok(await attendance.DecideCorrectionAsync(id, currentUser.EmployeeId, request, ct));

    private Guid RequireEmployeeId() =>
        currentUser.EmployeeId
        ?? throw new DomainRuleException("This account is not linked to an employee record yet.");
}
