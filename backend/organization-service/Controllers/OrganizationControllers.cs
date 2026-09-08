using HRM.OrganizationService.Application;
using HRM.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.OrganizationService.Controllers;

[ApiController]
[Route("api/departments")]
public class DepartmentsController(IOrganizationService organization) : ControllerBase
{
    /// <summary>Flat list, used to populate dropdowns across the app.</summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<DepartmentDto>>> GetAll(CancellationToken ct)
        => Ok(await organization.GetDepartmentsAsync(ct));

    /// <summary>Nested tree, used to render the org chart.</summary>
    [HttpGet("tree")]
    [Authorize]
    public async Task<ActionResult<List<DepartmentTreeNode>>> GetTree(CancellationToken ct)
        => Ok(await organization.GetDepartmentTreeAsync(ct));

    [HttpPost]
    [HasPermission(Permissions.OrganizationManage)]
    public async Task<ActionResult<DepartmentDto>> Create(SaveDepartmentRequest request, CancellationToken ct)
    {
        var created = await organization.CreateDepartmentAsync(request, ct);
        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.OrganizationManage)]
    public async Task<ActionResult<DepartmentDto>> Update(Guid id, SaveDepartmentRequest request, CancellationToken ct)
        => Ok(await organization.UpdateDepartmentAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.OrganizationManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await organization.DeleteDepartmentAsync(id, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/designations")]
public class DesignationsController(IOrganizationService organization) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<DesignationDto>>> GetAll([FromQuery] Guid? departmentId, CancellationToken ct)
        => Ok(await organization.GetDesignationsAsync(departmentId, ct));

    [HttpPost]
    [HasPermission(Permissions.OrganizationManage)]
    public async Task<ActionResult<DesignationDto>> Create(SaveDesignationRequest request, CancellationToken ct)
        => Ok(await organization.CreateDesignationAsync(request, ct));

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.OrganizationManage)]
    public async Task<ActionResult<DesignationDto>> Update(Guid id, SaveDesignationRequest request, CancellationToken ct)
        => Ok(await organization.UpdateDesignationAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.OrganizationManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await organization.DeleteDesignationAsync(id, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/branches")]
public class BranchesController(IOrganizationService organization) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<BranchDto>>> GetAll(CancellationToken ct)
        => Ok(await organization.GetBranchesAsync(ct));

    [HttpPost]
    [HasPermission(Permissions.OrganizationManage)]
    public async Task<ActionResult<BranchDto>> Create(SaveBranchRequest request, CancellationToken ct)
        => Ok(await organization.CreateBranchAsync(request, ct));

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.OrganizationManage)]
    public async Task<ActionResult<BranchDto>> Update(Guid id, SaveBranchRequest request, CancellationToken ct)
        => Ok(await organization.UpdateBranchAsync(id, request, ct));
}

[ApiController]
[Route("api/holidays")]
public class HolidaysController(IOrganizationService organization) : ControllerBase
{
    /// <summary>Read by Attendance and Leave to decide which days are working days.</summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<HolidayDto>>> GetAll([FromQuery] int? year, CancellationToken ct)
        => Ok(await organization.GetHolidaysAsync(year ?? DateTime.UtcNow.Year, ct));

    [HttpPost]
    [HasPermission(Permissions.OrganizationManage)]
    public async Task<ActionResult<HolidayDto>> Create(SaveHolidayRequest request, CancellationToken ct)
        => Ok(await organization.CreateHolidayAsync(request, ct));

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.OrganizationManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await organization.DeleteHolidayAsync(id, ct);
        return NoContent();
    }
}
