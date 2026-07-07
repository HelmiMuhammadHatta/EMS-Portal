using Asp.Versioning;
using EMS.Application.Leaves;
using EMS.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EMS.API.Controllers.v1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
[ApiController]
[Authorize] // All leave endpoints require authentication
public class LeavesController : ControllerBase
{
    private readonly ILeaveService _leaveService;

    public LeavesController(ILeaveService leaveService)
    {
        _leaveService = leaveService;
    }

    private Guid GetRequesterId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsRequesterAdmin() => User.FindFirstValue(ClaimTypes.Role) == "Admin";

    [HttpGet("leave-types")]
    public async Task<IActionResult> GetLeaveTypes()
    {
        var result = await _leaveService.GetLeaveTypesAsync();
        return Ok(ApiResponse<IEnumerable<LeaveTypeResponse>>.SuccessResponse(result));
    }

    [HttpGet("leave-requests")]
    public async Task<IActionResult> GetLeaveRequests([FromQuery] LeaveRequestListQuery query)
    {
        var result = await _leaveService.GetLeaveRequestsAsync(query, GetRequesterId(), IsRequesterAdmin());
        return Ok(ApiResponse<object>.SuccessResponse(result));
    }

    [HttpPost("leave-requests")]
    public async Task<IActionResult> CreateLeaveRequest([FromBody] CreateLeaveRequest request, [FromServices] IValidator<CreateLeaveRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(ApiResponse<object>.FailureResponse("Validation Failed", validationResult.Errors.Select(e => e.ErrorMessage).ToList()));

        var id = await _leaveService.CreateLeaveRequestAsync(request, GetRequesterId());
        return Ok(ApiResponse<object>.SuccessResponse(new { Id = id }, "Leave request created successfully."));
    }

    [Authorize(Policy = "leave.approve")]
    [HttpPut("leave-requests/{id}/approve")]
    public async Task<IActionResult> ApproveLeaveRequest(Guid id)
    {
        await _leaveService.ApproveLeaveRequestAsync(id, GetRequesterId(), IsRequesterAdmin());
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Leave request approved."));
    }

    [Authorize(Policy = "leave.approve")]
    [HttpPut("leave-requests/{id}/reject")]
    public async Task<IActionResult> RejectLeaveRequest(Guid id, [FromBody] RejectLeaveRequest request, [FromServices] IValidator<RejectLeaveRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(ApiResponse<object>.FailureResponse("Validation Failed", validationResult.Errors.Select(e => e.ErrorMessage).ToList()));

        await _leaveService.RejectLeaveRequestAsync(id, request, GetRequesterId(), IsRequesterAdmin());
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Leave request rejected."));
    }

    [HttpDelete("leave-requests/{id}")]
    public async Task<IActionResult> CancelLeaveRequest(Guid id)
    {
        await _leaveService.CancelLeaveRequestAsync(id, GetRequesterId());
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Leave request cancelled."));
    }

    [HttpGet("leave-balances/{employeeId}")]
    public async Task<IActionResult> GetLeaveBalances(Guid employeeId, [FromQuery] int year)
    {
        if (year == 0) year = DateTime.UtcNow.Year;
        var result = await _leaveService.GetLeaveBalancesAsync(employeeId, year);
        return Ok(ApiResponse<IEnumerable<LeaveBalanceResponse>>.SuccessResponse(result));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("leave-balances/initialize")]
    public async Task<IActionResult> InitializeLeaveBalances([FromQuery] int year)
    {
        if (year == 0) year = DateTime.UtcNow.Year;
        await _leaveService.InitializeLeaveBalancesAsync(year);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, $"Leave balances initialized for year {year}."));
    }
}
