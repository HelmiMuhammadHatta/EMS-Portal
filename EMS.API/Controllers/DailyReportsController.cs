using System;
using System.Threading.Tasks;
using EMS.Application.DailyReports;
using EMS.API.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/v1/daily-reports")]
public class DailyReportsController : ControllerBase
{
    private readonly IDailyReportService _dailyReportService;

    public DailyReportsController(IDailyReportService dailyReportService)
    {
        _dailyReportService = dailyReportService;
    }

    private Guid GetUserId()
    {
        var idClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(idClaim!);
    }

    private bool IsAdmin()
    {
        return User.IsInRole("Admin");
    }

    [HttpGet]
    [Authorize(Policy = "dailyreport.read")]
    public async Task<IActionResult> GetDailyReports([FromQuery] DailyReportListQuery query)
    {
        var result = await _dailyReportService.GetDailyReportsAsync(query, GetUserId(), IsAdmin());
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "dailyreport.read")]
    public async Task<IActionResult> GetDailyReport(Guid id)
    {
        var result = await _dailyReportService.GetDailyReportByIdAsync(id, GetUserId(), IsAdmin());
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "dailyreport.write")]
    public async Task<IActionResult> CreateDailyReport([FromBody] CreateDailyReportDto request)
    {
        var result = await _dailyReportService.CreateDailyReportAsync(request, GetUserId());
        return CreatedAtAction(nameof(GetDailyReport), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "dailyreport.write")]
    public async Task<IActionResult> UpdateDailyReport(Guid id, [FromBody] UpdateDailyReportDto request)
    {
        var result = await _dailyReportService.UpdateDailyReportAsync(id, request, GetUserId());
        return Ok(result);
    }

    [HttpPut("{id}/review")]
    [Authorize(Policy = "dailyreport.review")]
    public async Task<IActionResult> ReviewDailyReport(Guid id, [FromBody] ReviewDailyReportDto request)
    {
        var result = await _dailyReportService.ReviewDailyReportAsync(id, request, GetUserId());
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "dailyreport.write")]
    public async Task<IActionResult> DeleteDailyReport(Guid id)
    {
        await _dailyReportService.DeleteDailyReportAsync(id, GetUserId());
        return NoContent();
    }
}
