using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using EMS.Application.Candidates;
using EMS.Application.JobOpenings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers.v1;

[ApiController]
[Route("api/v1/public")]
[AllowAnonymous]
public class PublicController : ControllerBase
{
    private readonly IJobOpeningService _jobOpeningService;
    private readonly ICandidateService _candidateService;

    public PublicController(IJobOpeningService jobOpeningService, ICandidateService candidateService)
    {
        _jobOpeningService = jobOpeningService;
        _candidateService = candidateService;
    }

    [HttpGet("job-openings")]
    public async Task<IActionResult> GetJobOpenings()
    {
        var jobOpenings = await _jobOpeningService.GetPublicJobOpeningsAsync();
        return Ok(jobOpenings);
    }

    [HttpGet("job-openings/{id}")]
    public async Task<IActionResult> GetJobOpeningById(Guid id)
    {
        try
        {
            var jobOpening = await _jobOpeningService.GetPublicJobOpeningByIdAsync(id);
            return Ok(jobOpening);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("apply")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ApplyJob([FromForm] PublicApplyFormModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (model.CvFile == null || model.CvFile.Length == 0)
            {
                return BadRequest(new { message = "File CV wajib diunggah." });
            }

            if (model.IjazahFile == null || model.IjazahFile.Length == 0)
            {
                return BadRequest(new { message = "File Ijazah wajib diunggah." });
            }

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                clientIp = forwardedFor.ToString().Split(',')[0].Trim();
            }

            using var cvStream = new MemoryStream();
            await model.CvFile.CopyToAsync(cvStream);
            cvStream.Position = 0;

            using var ijazahStream = new MemoryStream();
            await model.IjazahFile.CopyToAsync(ijazahStream);
            ijazahStream.Position = 0;

            var serviceRequest = new PublicApplyServiceRequest(
                model.FullName,
                model.Email,
                model.Phone,
                model.JobOpeningId,
                model.Education,
                model.WorkExperience,
                new UploadCandidateFileItem(cvStream, model.CvFile.FileName, model.CvFile.Length, "CV"),
                new UploadCandidateFileItem(ijazahStream, model.IjazahFile.FileName, model.IjazahFile.Length, "Ijazah")
            );

            var result = await _candidateService.SubmitPublicApplicationAsync(serviceRequest, clientIp);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class PublicApplyFormModel
{
    [Required(ErrorMessage = "Nama lengkap wajib diisi.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email wajib diisi.")]
    [EmailAddress(ErrorMessage = "Format email tidak valid.")]
    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    [Required(ErrorMessage = "Lowongan pekerjaan harus dipilih.")]
    public Guid JobOpeningId { get; set; }

    public string? Education { get; set; }
    public string? WorkExperience { get; set; }

    [Required(ErrorMessage = "File CV wajib diunggah.")]
    public IFormFile CvFile { get; set; } = null!;

    [Required(ErrorMessage = "File Ijazah wajib diunggah.")]
    public IFormFile IjazahFile { get; set; } = null!;
}
