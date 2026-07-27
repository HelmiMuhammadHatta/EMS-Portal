using EMS.Application.Attendances;
using EMS.Application.Interfaces;
using EMS.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace EMS.API.Controllers.v1;

[ApiController]
[Route("api/v1/shift-schedules")]
[Authorize]
public class ShiftSchedulesController : ControllerBase
{
    private readonly IWorkShiftService _workShiftService;
    private readonly IShiftRotationService _rotationService;
    private readonly ICurrentUserService _currentUserService;

    public ShiftSchedulesController(IWorkShiftService workShiftService, IShiftRotationService rotationService, ICurrentUserService currentUserService)
    {
        _workShiftService = workShiftService;
        _rotationService = rotationService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSchedules([FromQuery] Guid? employeeId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        if (startDate.Kind == DateTimeKind.Unspecified) startDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
        if (endDate.Kind == DateTimeKind.Unspecified) endDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);
        
        var result = await _workShiftService.GetShiftSchedulesAsync(employeeId, startDate, endDate);
        return Ok(ApiResponse<object>.SuccessResponse(result));
    }

    [HttpPost]
    [Authorize(Policy = "RequireManagerRole")] // Manager or Admin
    public async Task<IActionResult> AssignSchedule(AssignShiftScheduleRequest request)
    {
        var userId = _currentUserService.UserId;
        if (userId == null || userId == Guid.Empty) return Unauthorized();
        
        await _workShiftService.AssignShiftScheduleAsync(request, userId.Value);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Schedule assigned successfully."));
    }

    [HttpPost("generate")]
    [Authorize(Policy = "RequireManagerRole")]
    public async Task<IActionResult> GenerateSchedule([FromBody] GenerateScheduleRequest request)
    {
        await _rotationService.GenerateShiftScheduleAsync(request.StartDate, request.EndDate);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Schedule generated successfully."));
    }

    [HttpPut("{id}/override")]
    [Authorize(Policy = "RequireManagerRole")]
    public async Task<IActionResult> OverrideSchedule(Guid id, [FromBody] OverrideScheduleRequest request)
    {
        await _workShiftService.OverrideShiftAsync(id, request.WorkShiftId);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Schedule overridden successfully."));
    }
}

public record GenerateScheduleRequest(DateTime StartDate, DateTime EndDate);
public record OverrideScheduleRequest(Guid WorkShiftId);
