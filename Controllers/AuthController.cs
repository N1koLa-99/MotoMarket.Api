using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MotoMarket.Api.Infrastructure;
using MotoMarket.Api.Models.Requests;
using MotoMarket.Api.Services.Interfaces;

namespace MotoMarket.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly JwtOptions _jwtOptions;

    public AuthController(IAuthService authService, IOptions<JwtOptions> jwtOptions)
    {
        _authService = authService;
        _jwtOptions = jwtOptions.Value;
    }

    [HttpPost("register/private")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterPrivate([FromBody] RegisterPrivateRequest request)
    {
        var response = await _authService.RegisterPrivateAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        return Ok(response);
    }

    [HttpPost("register/company")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterCompany([FromBody] RegisterCompanyRequest request)
    {
        var response = await _authService.RegisterCompanyAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        return Ok(response);
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        await _authService.VerifyEmailAsync(request);

        return Ok(new
        {
            success = true,
            message = "Имейлът е потвърден успешно. Вече можеш да влезеш в профила си."
        });
    }

    [HttpPost("resend-email-code")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendEmailCode([FromBody] ResendEmailVerificationRequest request)
    {
        await _authService.ResendEmailVerificationAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return Ok(new
        {
            success = true,
            message = "Ако имейлът съществува и не е потвърден, изпратихме нов код."
        });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _authService.ForgotPasswordAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return Ok(new
        {
            success = true,
            message = "Ако има профил с този имейл, изпратихме код за смяна на парола."
        });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _authService.ResetPasswordAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return Ok(new
        {
            success = true,
            message = "Паролата е сменена успешно. Влез с новата парола."
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var refreshToken = _authService.ConsumeLatestRefreshToken();
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            CookieHelper.SetRefreshTokenCookie(Response, refreshToken, _jwtOptions.RefreshTokenDays);
        }

        return Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest? request)
    {
        var refreshToken =
            request?.RefreshToken
            ?? Request.Cookies[CookieHelper.RefreshTokenCookieName];

        var response = await _authService.RefreshAsync(
            refreshToken ?? string.Empty,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var newRefreshToken = _authService.ConsumeLatestRefreshToken();
        if (!string.IsNullOrWhiteSpace(newRefreshToken))
        {
            CookieHelper.SetRefreshTokenCookie(Response, newRefreshToken, _jwtOptions.RefreshTokenDays);
        }

        return Ok(response);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies[CookieHelper.RefreshTokenCookieName];

        await _authService.LogoutAsync(
            refreshToken ?? string.Empty,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        CookieHelper.DeleteRefreshTokenCookie(Response);

        return Ok(new
        {
            message = "Успешен logout."
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.GetUserId();
        var me = await _authService.GetMeAsync(userId);

        return Ok(me);
    }

    [Authorize]
    [HttpPut("profile/private")]
    public async Task<IActionResult> UpdatePrivateProfile([FromBody] UpdatePrivateProfileRequest request)
    {
        var userId = User.GetUserId();
        var profile = await _authService.UpdatePrivateProfileAsync(userId, request);

        return Ok(profile);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.GetUserId();

        await _authService.ChangePasswordAsync(
            userId,
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        CookieHelper.DeleteRefreshTokenCookie(Response);

        return Ok(new
        {
            message = "Паролата е сменена успешно. Влез отново."
        });
    }

    [Authorize]
    [HttpPost("change-email")]
    public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequest request)
    {
        var userId = User.GetUserId();

        await _authService.ChangeEmailAsync(
            userId,
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        CookieHelper.DeleteRefreshTokenCookie(Response);

        return Ok(new
        {
            message = "Имейлът е сменен успешно. Влез отново."
        });
    }

    [Authorize]
    [HttpDelete("delete-profile")]
    public async Task<IActionResult> DeleteProfile([FromBody] DeleteProfileRequest request)
    {
        var userId = User.GetUserId();

        await _authService.DeleteMyProfileAsync(userId, request);

        CookieHelper.DeleteRefreshTokenCookie(Response);

        return Ok(new
        {
            message = "Профилът е изтрит успешно."
        });
    }
}