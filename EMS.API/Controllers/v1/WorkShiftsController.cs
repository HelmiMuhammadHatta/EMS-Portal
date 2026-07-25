using EMS.Application.Attendances;
using EMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace EMS.API.Controllers.v1;

[ApiController]
[Route("api/v1/work-shifts")]
[Authorize]
public class WorkShiftsController : ControllerBase
{
    private readonly IWorkShiftService _workShiftService;
    private readonly ICurrentUserService _currentUserService;

    public WorkShiftsController(IWorkShiftService workShiftService, ICurrentUserService currentUserService)
    {
        _workShiftService = workShiftService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _workShiftService.GetAllWorkShiftsAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _workShiftService.GetWorkShiftByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> Create(CreateWorkShiftRequest request)
    {
        var result = await _workShiftService.CreateWorkShiftAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> Update(Guid id, UpdateWorkShiftRequest request)
    {
        try
        {
            var result = await _workShiftService.UpdateWorkShiftAsync(id, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _workShiftService.DeleteWorkShiftAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}
