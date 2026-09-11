using System.Security.Cryptography;
using System.Text;
using MercatoApplication.Common;
using MercatoApplication.Interfaces;
using MercatoDomain.Entities;
using MercatoInfrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MercatoInfrastructure.Authentication;
public class RefreshTokenService : IRefreshTokenService
{
    private readonly AppDbContext _db; 
    private readonly JwtSettings _jwtSettings;
    public RefreshTokenService(AppDbContext db, IOptions<JwtSettings> jwtOptions)
    {
        _db = db;
        _jwtSettings = jwtOptions.Value;
    }
    public async Task<(string PlainToken, RefreshToken Entity)> CreateRefreshTokenAsync(Guid userId, Guid? familyId = null)
    { 
        // Plain token = what we give to the client (in an HttpOnly cookie later). 
        // We NEVER store this plain value - only its hash.
        var plainToken = GenerateSecureRandomToken();
        var tokenHash = HashToken(plainToken);
        var entity = new RefreshToken 
        {
            Id = Guid.NewGuid(), 
            UserId = userId, 
            TokenHash = tokenHash,
            // New login = new family. Rotation (below) reuses the SAME family.
            FamilyId = familyId ?? Guid.NewGuid(),
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow };
        _db.RefreshTokens.Add(entity); 
        await _db.SaveChangesAsync();
        return (plainToken, entity);
    } 
    public async Task<RefreshTokenRotationResult> RotateRefreshTokenAsync(string plainToken)
    {
        var tokenHash = HashToken(plainToken);
        var existing = await _db.RefreshTokens
            .Where(rt => rt.TokenHash == tokenHash)
            .FirstOrDefaultAsync();
        if (existing is null)
            return RefreshTokenRotationResult.Fail();
        if (existing.IsRevoked) 
        {
            // This exact token was already used once before - someone is replaying an old token.
            // Treat this as a possible theft: revoke the ENTIRE family, force re-login.
            await RevokeTokenFamilyAsync(existing.FamilyId);
            return RefreshTokenRotationResult.Fail(reuseDetected: true);
        } 
        if (existing.ExpiresAt < DateTime.UtcNow)
            return RefreshTokenRotationResult.Fail();
        // Valid - rotate: revoke this one, issue a brand new one in the SAME family
        existing.IsRevoked = true; 
        var (newPlainToken, newEntity) = await CreateRefreshTokenAsync(existing.UserId, existing.FamilyId);
        existing.ReplacedByTokenId = newEntity.Id; 
        await _db.SaveChangesAsync();
        return RefreshTokenRotationResult.Ok(existing.UserId, newPlainToken);
    }
    public async Task RevokeTokenFamilyAsync(Guid familyId)
    {
        var tokens = await _db.RefreshTokens 
            .Where(rt => rt.FamilyId == familyId && !rt.IsRevoked) 
            .ToListAsync(); 
        foreach (var token in tokens) 
            token.IsRevoked = true;
        await _db.SaveChangesAsync();
    }
    private static string GenerateSecureRandomToken() 
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    
    }
    private static string HashToken(string token) 
    { 
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
    public async Task RevokeByPlainTokenAsync(string plainToken)
    {
        var tokenHash = HashToken(plainToken);
        var existing = await _db.RefreshTokens
            .Where(rt => rt.TokenHash == tokenHash)
            .FirstOrDefaultAsync();
        if (existing is not null && !existing.IsRevoked)
        {
            existing.IsRevoked = true;
            await _db.SaveChangesAsync();
        }
    }
}

