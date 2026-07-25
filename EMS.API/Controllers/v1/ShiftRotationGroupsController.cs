using EMS.Application.Attendances;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace EMS.API.Controllers.v1;

[ApiController]
[Route("api/v1/shift-rotation-groups")]
[Authorize]
public class ShiftRotationGroupsController : ControllerBase
{
    private readonly IShiftRotationService _rotationService;

    public ShiftRotationGroupsController(IShiftRotationService rotationService)
    {
        _rotationService = rotationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllGroups()
    {
        var groups = await _rotationService.GetAllGroupsAsync();
        return Ok(groups);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGroupById(Guid id)
    {
        var group = await _rotationService.GetGroupByIdAsync(id);
        if (group == null) return NotFound();
        return Ok(group);
    }

    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] CreateShiftRotationGroupRequest request)
    {
        var result = await _rotationService.CreateGroupAsync(request);
        return CreatedAtAction(nameof(GetGroupById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateGroup(Guid id, [FromBody] UpdateShiftRotationGroupRequest request)
    {
        var result = await _rotationService.UpdateGroupAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGroup(Guid id)
    {
        await _rotationService.DeleteGroupAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/assign-employees")]
    public async Task<IActionResult> AssignEmployees(Guid id, [FromBody] System.Collections.Generic.List<Guid> employeeIds)
    {
        await _rotationService.AssignEmployeesToGroupAsync(id, employeeIds);
        return Ok();
    }

    // Pattern Endpoints
    [HttpGet("{id}/patterns")]
    public async Task<IActionResult> GetPatterns(Guid id)
    {
        var patterns = await _rotationService.GetPatternsByGroupIdAsync(id);
        return Ok(patterns);
    }

    [HttpPost("{id}/patterns")]
    public async Task<IActionResult> CreatePattern(Guid id, [FromBody] CreateShiftRotationPatternRequest request)
    {
        if (request.RotationGroupId != id)
        {
            request = request with { RotationGroupId = id };
        }
        var result = await _rotationService.CreatePatternAsync(request);
        return Ok(result);
    }

    [HttpPut("{id}/patterns/{patternId}")]
    public async Task<IActionResult> UpdatePattern(Guid id, Guid patternId, [FromBody] UpdateShiftRotationPatternRequest request)
    {
        var result = await _rotationService.UpdatePatternAsync(patternId, request);
        return Ok(result);
    }

    [HttpDelete("{id}/patterns/{patternId}")]
    public async Task<IActionResult> DeletePattern(Guid id, Guid patternId)
    {
        await _rotationService.DeletePatternAsync(patternId);
        return NoContent();
    }
}
