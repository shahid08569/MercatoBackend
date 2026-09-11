using MercatoDomain.Entities;
namespace MercatoApplication.Interfaces;
public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GeneratePurposeToken(Guid userId, string purpose, int expiryMinutes);
    (bool IsValid, Guid UserId) ValidatePurposeToken(string token, string expectedPurpose);
}
