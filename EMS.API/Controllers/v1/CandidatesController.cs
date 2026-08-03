using EMS.Application.Candidates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace EMS.API.Controllers.v1;

[ApiController]
[Route("api/v1/candidates")]
[Authorize] // HR/Admin only
public class CandidatesController : ControllerBase
{
    private readonly ICandidateService _candidateService;

    public CandidatesController(ICandidateService candidateService)
    {
        _candidateService = candidateService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCandidates()
    {
        var candidates = await _candidateService.GetCandidatesAsync();
        return Ok(candidates);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCandidateById(Guid id)
    {
        try
        {
            var candidate = await _candidateService.GetCandidateByIdAsync(id);
            return Ok(candidate);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateCandidate([FromBody] CreateCandidateRequest request)
    {
        try
        {
            var candidate = await _candidateService.CreateCandidateAsync(request);
            return Ok(candidate);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCandidate(Guid id, [FromBody] UpdateCandidateRequest request)
    {
        try
        {
            await _candidateService.UpdateCandidateAsync(id, request);
            return Ok(new { message = "Candidate updated" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateCandidateStatus(Guid id, [FromBody] string status)
    {
        try
        {
            await _candidateService.UpdateCandidateStatusAsync(id, status);
            return Ok(new { message = "Status updated" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/assign-test")]
    public async Task<IActionResult> AssignTest(Guid id, [FromBody] AssignTestRequest request)
    {
        try
        {
            var links = await _candidateService.AssignTestAsync(id, request);
            return Ok(new { message = "Tests assigned", testLinks = links });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/test-results")]
    public async Task<IActionResult> GetTestResults(Guid id)
    {
        try
        {
            var results = await _candidateService.GetCandidateTestResultsAsync(id);
            return Ok(results);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/convert-to-employee")]
    public async Task<IActionResult> ConvertToEmployee(Guid id, [FromBody] ConvertToEmployeeRequest request)
    {
        try
        {
            var (employeeId, tempPassword) = await _candidateService.ConvertToEmployeeAsync(id, request);
            return Ok(new { 
                message = "Candidate converted to employee successfully", 
                employeeId, 
                tempPassword 
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/documents")]
    public async Task<IActionResult> GetCandidateDocuments(Guid id)
    {
        try
        {
            var docs = await _candidateService.GetCandidateDocumentsAsync(id);
            return Ok(docs);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/documents/{documentId}/download")]
    public async Task<IActionResult> DownloadCandidateDocument(Guid id, Guid documentId)
    {
        try
        {
            var (stream, contentType, fileName) = await _candidateService.DownloadCandidateDocumentAsync(id, documentId);
            return File(stream, contentType, fileName);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/documents/{documentId}/view")]
    public async Task<IActionResult> ViewCandidateDocument(Guid id, Guid documentId)
    {
        try
        {
            var (stream, contentType, fileName) = await _candidateService.DownloadCandidateDocumentAsync(id, documentId);
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
            return File(stream, contentType);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
