using Asp.Versioning;
using EMS.Application.Employees;
using EMS.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EMS.API.Controllers.v1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    private Guid GetRequesterId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsRequesterAdmin() => User.FindFirstValue(ClaimTypes.Role) == "Admin";

    [Authorize(Policy = "employee.read")]
    [HttpGet]
    public async Task<IActionResult> GetEmployees([FromQuery] EmployeeListRequest request)
    {
        var result = await _employeeService.GetEmployeesAsync(request, GetRequesterId(), IsRequesterAdmin());
        return Ok(ApiResponse<PaginatedResponse<EmployeeResponse>>.SuccessResponse(result));
    }

    [Authorize(Policy = "employee.read")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEmployeeById(Guid id)
    {
        var result = await _employeeService.GetEmployeeByIdAsync(id, GetRequesterId(), IsRequesterAdmin());
        return Ok(ApiResponse<EmployeeDetailResponse>.SuccessResponse(result));
    }

    [Authorize(Policy = "employee.write")]
    [HttpPost]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest request, [FromServices] IValidator<CreateEmployeeRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(ApiResponse<object>.FailureResponse("Validation Failed", validationResult.Errors.Select(e => e.ErrorMessage).ToList()));

        var result = await _employeeService.CreateEmployeeAsync(request);
        return Ok(ApiResponse<object>.SuccessResponse(new { Id = result.Id, TemporaryPassword = result.TempPassword }, "Employee created successfully."));
    }

    [Authorize(Policy = "employee.write")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEmployee(Guid id, [FromBody] UpdateEmployeeRequest request, [FromServices] IValidator<UpdateEmployeeRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(ApiResponse<object>.FailureResponse("Validation Failed", validationResult.Errors.Select(e => e.ErrorMessage).ToList()));

        await _employeeService.UpdateEmployeeAsync(id, request, GetRequesterId(), IsRequesterAdmin());
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Employee updated successfully."));
    }

    [Authorize(Policy = "employee.delete")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEmployee(Guid id)
    {
        await _employeeService.DeleteEmployeeAsync(id, GetRequesterId(), IsRequesterAdmin());
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Employee soft deleted successfully."));
    }

    [Authorize(Policy = "employee.read")]
    [HttpGet("{id}/subordinates")]
    public async Task<IActionResult> GetSubordinates(Guid id)
    {
        var result = await _employeeService.GetSubordinatesAsync(id, GetRequesterId(), IsRequesterAdmin());
        return Ok(ApiResponse<IEnumerable<EmployeeResponse>>.SuccessResponse(result));
    }

    [Authorize(Policy = "employee.read")]
    [HttpGet("{id}/hierarchy")]
    public async Task<IActionResult> GetHierarchy(Guid id)
    {
        var result = await _employeeService.GetHierarchyAsync(id, GetRequesterId(), IsRequesterAdmin());
        return Ok(ApiResponse<object>.SuccessResponse(result));
    }

    [Authorize(Policy = "employee.write")]
    [HttpPost("{id}/documents")]
    public async Task<IActionResult> UploadDocument(Guid id, IFormFile file, [FromForm] string documentType)
    {
        if (file == null || file.Length == 0) return BadRequest("File is empty");
        
        using var stream = file.OpenReadStream();
        await _employeeService.UploadDocumentAsync(id, stream, file.FileName, documentType, GetRequesterId(), IsRequesterAdmin());
        
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Document uploaded successfully."));
    }

    [Authorize(Policy = "employee.read")]
    [HttpGet("{id}/documents")]
    public async Task<IActionResult> GetDocuments(Guid id)
    {
        var result = await _employeeService.GetDocumentsAsync(id, GetRequesterId(), IsRequesterAdmin());
        return Ok(ApiResponse<IEnumerable<DocumentResponse>>.SuccessResponse(result));
    }

    [Authorize(Policy = "employee.read")]
    [HttpGet("{id}/documents/{documentId}/download")]
    public async Task<IActionResult> DownloadDocument(Guid id, Guid documentId)
    {
        var (stream, contentType, fileName) = await _employeeService.DownloadDocumentAsync(id, documentId, GetRequesterId(), IsRequesterAdmin());
        return File(stream, contentType, fileName);
    }

    [Authorize(Policy = "employee.write")]
    [HttpDelete("{id}/documents/{documentId}")]
    public async Task<IActionResult> DeleteDocument(Guid id, Guid documentId)
    {
        await _employeeService.DeleteDocumentAsync(id, documentId, GetRequesterId(), IsRequesterAdmin());
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Document deleted successfully."));
    }

    [Authorize(Policy = "employee.read")]
    [HttpGet("{id}/audit-log")]
    public async Task<IActionResult> GetAuditLogs(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _employeeService.GetAuditLogsAsync(id, page, pageSize, GetRequesterId(), IsRequesterAdmin());
        return Ok(ApiResponse<object>.SuccessResponse(result));
    }

    [Authorize(Policy = "employee.write")]
    [HttpPut("{id}/password")]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return BadRequest(ApiResponse<object>.FailureResponse("Validation Failed", new List<string> { "Password must be at least 6 characters long." }));

        await _employeeService.ChangePasswordAsync(id, request.NewPassword, GetRequesterId(), IsRequesterAdmin());
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Password changed successfully."));
    }
}
