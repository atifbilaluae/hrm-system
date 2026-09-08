using HRM.EmployeeService.Application;
using HRM.EmployeeService.Domain;
using HRM.Shared.Auth;
using HRM.Shared.Common;
using Microsoft.AspNetCore.Mvc;

namespace HRM.EmployeeService.Controllers;

/// <summary>
/// Controllers stay thin: bind, authorize, delegate, return. No business logic here.
/// </summary>
[ApiController]
[Route("api/employees")]
[Produces("application/json")]
public class EmployeesController(IEmployeeService employees) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.EmployeeView)]
    [ProducesResponseType<PagedResult<EmployeeListItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EmployeeListItemDto>>> Search(
        [FromQuery] string? search, [FromQuery] Guid? departmentId, [FromQuery] EmploymentStatus? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "joiningDate", [FromQuery] bool desc = true,
        CancellationToken ct = default)
        => Ok(await employees.SearchAsync(new EmployeeQuery(search, departmentId, status, page, pageSize, sortBy, desc), ct));

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.EmployeeView)]
    [ProducesResponseType<EmployeeDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeDetailDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await employees.GetByIdAsync(id, ct));

    [HttpGet("stats")]
    [HasPermission(Permissions.EmployeeView)]
    [ProducesResponseType<EmployeeStatsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeStatsDto>> Stats(CancellationToken ct)
        => Ok(await employees.GetStatsAsync(ct));

    [HttpPost]
    [HasPermission(Permissions.EmployeeCreate)]
    [ProducesResponseType<EmployeeDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeDetailDto>> Create(CreateEmployeeRequest request, CancellationToken ct)
    {
        var created = await employees.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.EmployeeUpdate)]
    public async Task<ActionResult<EmployeeDetailDto>> Update(Guid id, UpdateEmployeeRequest request, CancellationToken ct)
        => Ok(await employees.UpdateAsync(id, request, ct));

    [HttpPost("{id:guid}/terminate")]
    [HasPermission(Permissions.EmployeeUpdate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Terminate(Guid id, TerminateEmployeeRequest request, CancellationToken ct)
    {
        await employees.TerminateAsync(id, request, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.EmployeeDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await employees.DeleteAsync(id, ct);
        return NoContent();
    }
}
