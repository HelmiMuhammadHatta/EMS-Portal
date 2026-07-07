using Asp.Versioning;
using EMS.Application.Attendances;
using EMS.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EMS.API.Controllers.v1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
[ApiController]
[Authorize]
public class AttendancesController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendancesController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    private Guid GetRequesterId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsRequesterAdmin() => User.FindFirstValue(ClaimTypes.Role) == "Admin";

    [HttpPost("attendances/clock-in")]
    public async Task<IActionResult> ClockIn([FromBody] ClockInRequest request, [FromServices] IValidator<ClockInRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(ApiResponse<object>.FailureResponse("Validation Failed", validationResult.Errors.Select(e => e.ErrorMessage).ToList()));

        await _attendanceService.ClockInAsync(request, GetRequesterId());
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Clock in successful."));
    }

    [HttpPost("attendances/clock-out")]
    public async Task<IActionResult> ClockOut([FromBody] ClockOutRequest request, [FromServices] IValidator<ClockOutRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(ApiResponse<object>.FailureResponse("Validation Failed", validationResult.Errors.Select(e => e.ErrorMessage).ToList()));

        await _attendanceService.ClockOutAsync(request, GetRequesterId());
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Clock out successful."));
    }

    [HttpGet("attendances")]
    public async Task<IActionResult> GetAttendances([FromQuery] AttendanceListQuery query)
    {
        var result = await _attendanceService.GetAttendancesAsync(query, GetRequesterId(), IsRequesterAdmin());
        return Ok(ApiResponse<object>.SuccessResponse(result));
    }

    [HttpGet("attendances/summary/{employeeId}")]
    public async Task<IActionResult> GetSummary(Guid employeeId, [FromQuery] int month, [FromQuery] int year)
    {
        if (month == 0) month = DateTime.UtcNow.AddHours(7).Month;
        if (year == 0) year = DateTime.UtcNow.AddHours(7).Year;

        var result = await _attendanceService.GetSummaryAsync(employeeId, month, year, GetRequesterId(), IsRequesterAdmin());
        return Ok(ApiResponse<AttendanceSummaryResponse>.SuccessResponse(result));
    }

    [Authorize(Policy = "employee.read")] // Or remove if staff can export their own
    [HttpGet("attendances/export")]
    public async Task<IActionResult> ExportAttendances([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] Guid? departmentId)
    {
        var fileBytes = await _attendanceService.ExportAttendancesAsync(startDate, endDate, departmentId, GetRequesterId(), IsRequesterAdmin());
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Attendances_Export_{DateTime.UtcNow.AddHours(7):yyyyMMdd}.xlsx");
    }
}
