using Asp.Versioning;
using EMS.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers.v1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class TestController : ControllerBase
{
    [HttpGet("success")]
    public IActionResult GetSuccess()
    {
        var response = ApiResponse<string>.SuccessResponse("This is a successful response data.");
        return Ok(response);
    }

    [HttpGet("error")]
    public IActionResult GetError()
    {
        // This will be caught by the ExceptionHandlingMiddleware
        throw new Exception("This is a simulated unhandled exception!");
    }
}
