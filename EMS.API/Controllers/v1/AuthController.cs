using Asp.Versioning;
using EMS.Application.Auth;
using EMS.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace EMS.API.Controllers.v1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [EnableRateLimiting("AuthRateLimit")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, [FromServices] IValidator<RegisterRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(ApiResponse<object>.FailureResponse("Validation Failed", validationResult.Errors.Select(e => e.ErrorMessage).ToList()));

        await _authService.RegisterAsync(request);
        return Ok(ApiResponse<object>.SuccessResponse(new {}, "Registration successful. Please verify your email."));
    }

    [HttpPost("login")]
    [EnableRateLimiting("AuthRateLimit")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, [FromServices] IValidator<LoginRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(ApiResponse<object>.FailureResponse("Validation Failed", validationResult.Errors.Select(e => e.ErrorMessage).ToList()));

        var tokens = await _authService.LoginAsync(request);
        return Ok(ApiResponse<TokenResponse>.SuccessResponse(tokens, "Login successful."));
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken) || string.IsNullOrEmpty(request.AccessToken)) 
            return BadRequest(ApiResponse<object>.FailureResponse("Tokens required."));
        
        var tokens = await _authService.RefreshTokenAsync(request);
        return Ok(ApiResponse<TokenResponse>.SuccessResponse(tokens, "Token refreshed."));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out var userId))
        {
            await _authService.LogoutAsync(userId);
        }
        return Ok(ApiResponse<object>.SuccessResponse(new {}, "Logged out successfully."));
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _authService.ForgotPasswordAsync(request);
        return Ok(ApiResponse<object>.SuccessResponse(new {}, "If email exists, reset token sent."));
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, [FromServices] IValidator<ResetPasswordRequest> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(ApiResponse<object>.FailureResponse("Validation Failed", validationResult.Errors.Select(e => e.ErrorMessage).ToList()));

        await _authService.ResetPasswordAsync(request);
        return Ok(ApiResponse<object>.SuccessResponse(new {}, "Password reset successfully."));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            return Unauthorized();

        var user = await _authService.GetCurrentUserAsync(userId);
        return Ok(ApiResponse<object>.SuccessResponse(user, "User profile retrieved."));
    }
    
    // Testing dynamic policy
    [Authorize(Policy = "employee.write")]
    [HttpPost("test-policy")]
    public IActionResult TestPolicy()
    {
        return Ok(ApiResponse<object>.SuccessResponse(new {}, "You have employee.write permission."));
    }
}
