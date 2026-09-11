using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MercatoApplication.Common;
using MercatoApplication.Interfaces;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Authentication;
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;
    public JwtTokenService(IOptions<JwtSettings> jwtOptions) 
    {
        _jwtSettings = jwtOptions.Value;
    }
    public string GenerateAccessToken(User user)
    {
        // Claims = pieces of information embedded inside the token itself
        var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new(JwtRegisteredClaimNames.Email, user.Email),
        new(ClaimTypes.Role, user.Role.Name),
        // Used later by [Authorize(Roles = "Admin")]
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)
        );

        var creds = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                _jwtSettings.AccessTokenExpirationMinutes
            ),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    public string GeneratePurposeToken(Guid userId, string purpose, int expiryMinutes)
    {
        var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, userId.ToString()),

        new("purpose", purpose),
        // Custom claim - marks WHY this token exists

        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)
        );

        var creds = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }


    public (bool IsValid, Guid UserId) ValidatePurposeToken(
        string token,
        string expectedPurpose)
    {
        var handler = new JwtSecurityTokenHandler();

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)
        );

        try
        {
            var principal = handler.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,

                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                },
                out _
            );

            var purpose = principal.FindFirst("purpose")?.Value;
            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (purpose != expectedPurpose || sub is null)
            {
                return (false, Guid.Empty);
            }

            return (true, Guid.Parse(sub));
        }
        catch
        {
            // Any validation failure
            // (expired, tampered, wrong signature) = simply invalid

            return (false, Guid.Empty);
        }
    }
}
