using EMS.Application.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        var result = await _roleService.GetRolesAsync();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole(CreateRoleDto request)
    {
        var result = await _roleService.CreateRoleAsync(request);
        return Ok(result);
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions()
    {
        var result = await _roleService.GetPermissionsAsync();
        return Ok(result);
    }

    [HttpGet("{id}/permissions")]
    public async Task<IActionResult> GetRolePermissions(Guid id)
    {
        var result = await _roleService.GetRolePermissionsAsync(id);
        return Ok(result);
    }

    [HttpPost("{id}/permissions")]
    public async Task<IActionResult> AssignPermissions(Guid id, AssignPermissionsDto request)
    {
        await _roleService.AssignPermissionsAsync(id, request);
        return Ok(new { message = "Permissions assigned successfully." });
    }
}
