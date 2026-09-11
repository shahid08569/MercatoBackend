
using MercatoApplication.DTOs.Auth;

namespace MercatoApplication.Common;

public class AuthResult 
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public AuthResponseDto? Response { get; set; }
    public string? PlainRefreshToken { get; set; }
    public static AuthResult Fail(string error) => new()
    { 
        Success = false,
        Error = error 
    }; 
    public static AuthResult Ok(AuthResponseDto response, string plainRefreshToken) => new() 
    { 
        Success = true,
        Response = response,
        PlainRefreshToken = plainRefreshToken
    };
}