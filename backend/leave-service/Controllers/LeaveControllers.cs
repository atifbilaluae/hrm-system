using HRM.LeaveService.Application;
using HRM.LeaveService.Domain;
using HRM.Shared.Auth;
using HRM.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.LeaveService.Controllers;

[ApiController]
[Route("api/leave-types")]
[Authorize]
public class LeaveTypesController(ILeaveService leave) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<LeaveTypeDto>>> GetAll(CancellationToken ct)
        => Ok(await leave.GetLeaveTypesAsync(ct));

    [HttpPost]
    [HasPermission(Permissions.LeaveApprove)]
    public async Task<ActionResult<LeaveTypeDto>> Create(SaveLeaveTypeRequest request, CancellationToken ct)
        => Ok(await leave.CreateLeaveTypeAsync(request, ct));

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.LeaveApprove)]
    public async Task<ActionResult<LeaveTypeDto>> Update(Guid id, SaveLeaveTypeRequest request, CancellationToken ct)
        => Ok(await leave.UpdateLeaveTypeAsync(id, request, ct));
}

[ApiController]
[Route("api/leave")]
[Authorize]
public class LeaveController(ILeaveService leave, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>My balances for the year, created on the fly if they do not exist yet.</summary>
    [HttpGet("balances")]
    public async Task<ActionResult<List<LeaveBalanceDto>>> MyBalances([FromQuery] int? year, CancellationToken ct)
        => Ok(await leave.GetBalancesAsync(RequireEmployeeId(), year ?? DateTime.UtcNow.Year, ct));

    [HttpGet("balances/{employeeId:guid}")]
    [HasPermission(Permissions.LeaveApprove)]
    public async Task<ActionResult<List<LeaveBalanceDto>>> BalancesFor(
        Guid employeeId, [FromQuery] int? year, CancellationToken ct)
        => Ok(await leave.GetBalancesAsync(employeeId, year ?? DateTime.UtcNow.Year, ct));

    [HttpGet("requests/me")]
    public async Task<ActionResult<PagedResult<LeaveRequestDto>>> MyRequests(
        [FromQuery] LeaveRequestStatus? status, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await leave.GetRequestsAsync(RequireEmployeeId(), status, page, pageSize, ct));

    /// <summary>The approver queue. Anyone with leave.approve sees pending requests.</summary>
    [HttpGet("requests/pending")]
    [HasPermission(Permissions.LeaveApprove)]
    public async Task<ActionResult<PagedResult<LeaveRequestDto>>> Pending(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await leave.GetRequestsAsync(null, LeaveRequestStatus.Pending, page, pageSize, ct));

    [HttpGet("requests")]
    [HasPermission(Permissions.LeaveApprove)]
    public async Task<ActionResult<PagedResult<LeaveRequestDto>>> All(
        [FromQuery] Guid? employeeId, [FromQuery] LeaveRequestStatus? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await leave.GetRequestsAsync(employeeId, status, page, pageSize, ct));

    [HttpPost("requests")]
    [HasPermission(Permissions.LeaveRequest)]
    [ProducesResponseType<LeaveRequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LeaveRequestDto>> Submit(CreateLeaveRequest request, CancellationToken ct)
    {
        // The manager id would come from Employee Service in a fuller implementation;
        // for now the request is visible to everyone holding leave.approve.
        var managerId = (Guid?)null;
        return Ok(await leave.SubmitAsync(RequireEmployeeId(), managerId, request, ct));
    }

    [HttpPost("requests/{id:guid}/decide")]
    [HasPermission(Permissions.LeaveApprove)]
    public async Task<ActionResult<LeaveRequestDto>> Decide(Guid id, DecideLeaveRequest request, CancellationToken ct)
        => Ok(await leave.DecideAsync(id, currentUser.EmployeeId, request, ct));

    [HttpPost("requests/{id:guid}/cancel")]
    public async Task<ActionResult<LeaveRequestDto>> Cancel(Guid id, CancellationToken ct)
        => Ok(await leave.CancelAsync(id, RequireEmployeeId(), ct));

    [HttpGet("calendar")]
    public async Task<ActionResult<List<LeaveCalendarEntryDto>>> Calendar(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var start = from ?? DateTime.UtcNow.Date.AddDays(-15);
        return Ok(await leave.GetCalendarAsync(start, to ?? start.AddDays(60), ct));
    }

    private Guid RequireEmployeeId() =>
        currentUser.EmployeeId
        ?? throw new DomainRuleException("This account is not linked to an employee record yet.");
}
