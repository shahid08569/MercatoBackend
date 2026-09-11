using MercatoApplication.Common;
using MercatoDomain.Entities;

namespace MercatoApplication.Interfaces;
public interface IRefreshTokenService
{
    Task<(string PlainToken, RefreshToken Entity)> CreateRefreshTokenAsync(
        Guid userId,
        Guid? familyId = null
    );

    Task<RefreshTokenRotationResult> RotateRefreshTokenAsync(
        string plainToken
    );

    Task RevokeTokenFamilyAsync(
        Guid familyId
    );
    Task RevokeByPlainTokenAsync(
        string plainToken
    );
}
