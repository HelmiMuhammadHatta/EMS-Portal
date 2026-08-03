using EMS.Application.Assessments;
using EMS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace EMS.API.Controllers.v1;

[ApiController]
[Route("api/v1/assessments")]
public class AssessmentsController : ControllerBase
{
    private readonly IAssessmentService _assessmentService;

    public AssessmentsController(IAssessmentService assessmentService)
    {
        _assessmentService = assessmentService;
    }

    [HttpGet("tests")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTests([FromQuery] TestType? type)
    {
        var tests = await _assessmentService.GetTestsAsync(type);
        return Ok(tests);
    }

    [HttpGet("tests/{id}/questions")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTestQuestions(Guid id)
    {
        try
        {
            var questions = await _assessmentService.GetTestQuestionsAsync(id);
            return Ok(questions);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("sessions")]
    [AllowAnonymous]
    public async Task<IActionResult> StartSession([FromBody] StartTestSessionRequest request)
    {
        try
        {
            var session = await _assessmentService.StartSessionAsync(request);
            return Ok(session);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("sessions/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSession(Guid id)
    {
        try
        {
            var session = await _assessmentService.GetTestSessionAsync(id);
            return Ok(session);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("sessions/{id}/verify-access")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyAccessCode(Guid id, [FromBody] VerifyAccessCodeRequest request)
    {
        try
        {
            var result = await _assessmentService.VerifyAccessCodeAsync(id, request.AccessCode);
            if (result.Success) return Ok(result);
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("sessions/{id}/proctoring-snapshot")]
    [AllowAnonymous]
    public async Task<IActionResult> UploadProctoringSnapshot(Guid id, [FromBody] UploadSnapshotRequest request)
    {
        try
        {
            await _assessmentService.UploadProctoringSnapshotAsync(id, request.Base64Image);
            return Ok(new { message = "Snapshot uploaded" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("sessions/{id}/tab-switch")]
    [AllowAnonymous]
    public async Task<IActionResult> RecordTabSwitch(Guid id, [FromBody] RecordTabSwitchRequest request)
    {
        try
        {
            await _assessmentService.RecordTabSwitchAsync(id, request.TabSwitchCount);
            return Ok(new { message = "Tab switch recorded" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("sessions/{id}/answers")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitAnswer(Guid id, [FromBody] SubmitAnswerRequest request)
    {
        try
        {
            await _assessmentService.SubmitAnswerAsync(id, request);
            return Ok(new { message = "Answer submitted" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("sessions/{id}/submit")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitSession(Guid id)
    {
        try
        {
            var result = await _assessmentService.SubmitSessionAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("sessions/{id}/result")]
    [Authorize] // HR/Admin can access this, or the employee themselves if authorized
    public async Task<IActionResult> GetTestResult(Guid id)
    {
        try
        {
            var result = await _assessmentService.GetTestResultAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
