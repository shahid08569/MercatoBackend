using MercatoApplication.Common;
using MercatoApplication.DTOs;

namespace MercatoApplication.Interfaces;
public interface IAuthService 
{
    Task<AuthResult> RegisterAsync(RegisterRequestDto request);
    Task<AuthResult> LoginAsync(LoginRequestDto request);
    Task<AuthResult> RefreshAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
    Task<string?> RequestEmailVerificationAsync(string email);
    Task<bool> VerifyEmailAsync(string token);
    Task<string?> RequestPasswordResetAsync(string email); 
    Task<bool> ResetPasswordAsync(string token, string newPassword);
}
