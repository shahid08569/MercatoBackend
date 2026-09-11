using MercatoApplication.Common;
using MercatoApplication.DTOs;
using MercatoApplication.DTOs.Auth;
using MercatoApplication.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MercatoAPI.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    [EnableRateLimiting("fixed")] // reuses the rate limiter policy from Phase 3
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        //Constructor to inject dependencies
        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequestDto request)
        {
            var result = await _authService.RegisterAsync(request);

            if (!result.Success)
                return BadRequest(
                    ApiResponse<object>.FailureResponse(result.Error!)
                );

            SetRefreshTokenCookie(result.PlainRefreshToken!);

            return Ok(
                ApiResponse<AuthResponseDto>.SuccessResponse(result.Response!)
            );
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequestDto request)
        {
            var result = await _authService.LoginAsync(request);

            if (!result.Success)
                return Unauthorized(
                    ApiResponse<object>.FailureResponse(result.Error!)
                );

            SetRefreshTokenCookie(result.PlainRefreshToken!);

            return Ok(
                ApiResponse<AuthResponseDto>.SuccessResponse(result.Response!)
            );
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized(
                    ApiResponse<object>.FailureResponse(
                        "No refresh token found."
                    )
                );
            }

            var result = await _authService.RefreshAsync(refreshToken);

            if (!result.Success)
            {
                return Unauthorized(
                    ApiResponse<object>.FailureResponse(
                        result.Error!
                    )
                );
            }

            SetRefreshTokenCookie(result.PlainRefreshToken!);

            return Ok(
                ApiResponse<AuthResponseDto>.SuccessResponse(
                    result.Response!
                )
            );
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _authService.LogoutAsync(refreshToken);
            }

            Response.Cookies.Delete("refreshToken");

            return Ok(
                ApiResponse.SuccessResponse()
            );
        }
        
        [HttpPost("send-verification-email")]
        public async Task<IActionResult> SendVerificationEmail([FromBody] EmailRequestDto request)
        {
            var token = await _authService
                .RequestEmailVerificationAsync(request.Email);

            if (token is not null)
            {
                var link = $"http://localhost:4200/verify-email?token={token}";

                // Real email sending comes in a later phase.
                _logger.LogInformation(
                    "Email verification link: {Link}",
                    link
                );
            }

            // Always return success to prevent account enumeration.
            return Ok(ApiResponse.SuccessResponse());
        }

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            var success = await _authService.VerifyEmailAsync(token);

            if (!success)
                return BadRequest(
                    ApiResponse<object>.FailureResponse(
                        "Invalid or expired verification link."
                    )
                );

            return Ok(ApiResponse.SuccessResponse());
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] EmailRequestDto request)
        {
            var token = await _authService
                .RequestPasswordResetAsync(request.Email);

            if (token is not null)
            {
                var link =
                    $"http://localhost:4200/reset-password?token={token}";

                _logger.LogInformation(
                    "Password reset link: {Link}",
                    link
                );
            }

            // Always return generic success to prevent account enumeration.
            return Ok(
                ApiResponse.SuccessResponse()
            );
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            var success = await _authService
                .ResetPasswordAsync(
                    request.Token,
                    request.NewPassword
                );

            if (!success)
            {
                return BadRequest(
                    ApiResponse<object>.FailureResponse(
                        "Invalid or expired reset link."
                    )
                );
            }

            return Ok(
                ApiResponse.SuccessResponse()
            );
        }
        private void SetRefreshTokenCookie(string plainRefreshToken)
        {
            Response.Cookies.Append(
                "refreshToken",
                plainRefreshToken,
                new CookieOptions
                {
                    HttpOnly = true, // JavaScript can NEVER read this - protects against XSS token theft
                    Secure = true, // only ever sent over HTTPS
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                }
            );
        }
    }
}