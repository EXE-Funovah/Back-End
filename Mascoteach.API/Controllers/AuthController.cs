using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mascoteach.Service.Interfaces;
using Mascoteach.Service.DTOs;

namespace Mascoteach.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        if (result == null) return BadRequest("Email existed.");
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        AuthResponse? result;
        try
        {
            result = await _authService.LoginAsync(request);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        if (result == null) return Unauthorized("Email or Password incorrect.");
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin(GoogleLoginRequest request)
    {
        var result = await _authService.GoogleLoginAsync(request);
        if (result == null) return Unauthorized("Google login failed.");
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        await _authService.ForgotPasswordAsync(request);
        return Ok(new { message = "If this email exists, a reset link has been sent." });
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        try
        {
            var success = await _authService.ResetPasswordAsync(request);
            if (!success) return BadRequest("Invalid or expired reset token.");
            return Ok("Password reset successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
