
namespace MercatoApplication.Common;
// Result of trying to use a refresh token - success, failure, or "reuse detected" (possible theft) 
public class RefreshTokenRotationResult
{
    public bool Success { get; set; }
    public bool ReuseDetected { get; set; }
    public Guid? UserId { get; set; }
    public string? NewPlainToken { get; set; }
    public static RefreshTokenRotationResult Fail(bool reuseDetected = false) => new()
    { 
        Success = false,
        ReuseDetected = reuseDetected 
    };
    public static RefreshTokenRotationResult Ok(
        Guid userId,
        string newPlainToken
        ) => new() 
        { 
            Success = true,
            UserId = userId, 
            NewPlainToken = newPlainToken
        };
}
