using System;
using System.Threading.Tasks;
using EMS.Application.JobOpenings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers.v1;

[ApiController]
[Route("api/v1/job-openings")]
[Authorize] // HR / Admin
public class JobOpeningsController : ControllerBase
{
    private readonly IJobOpeningService _jobOpeningService;

    public JobOpeningsController(IJobOpeningService jobOpeningService)
    {
        _jobOpeningService = jobOpeningService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllJobOpenings()
    {
        var list = await _jobOpeningService.GetAllJobOpeningsAsync();
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetJobOpeningById(Guid id)
    {
        try
        {
            var job = await _jobOpeningService.GetJobOpeningByIdAsync(id);
            return Ok(job);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateJobOpening([FromBody] CreateJobOpeningRequest request)
    {
        try
        {
            var created = await _jobOpeningService.CreateJobOpeningAsync(request);
            return Ok(created);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateJobOpening(Guid id, [FromBody] UpdateJobOpeningRequest request)
    {
        try
        {
            await _jobOpeningService.UpdateJobOpeningAsync(id, request);
            return Ok(new { message = "Job Opening berhasil diperbarui." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        try
        {
            await _jobOpeningService.ToggleJobOpeningStatusAsync(id);
            return Ok(new { message = "Status lowongan berhasil diubah." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteJobOpening(Guid id)
    {
        try
        {
            await _jobOpeningService.DeleteJobOpeningAsync(id);
            return Ok(new { message = "Job Opening berhasil dihapus." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
