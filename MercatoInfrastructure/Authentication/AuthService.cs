using MercatoApplication.Common;
using MercatoApplication.DTOs;
using MercatoApplication.DTOs.Auth;
using MercatoApplication.Interfaces;
using MercatoDomain.Entities;
using MercatoInfrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace MercatoInfrastructure.Authentication;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHashingService _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    // Constructor to inject dependencies
    public AuthService(
        AppDbContext db,
        IPasswordHashingService passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
    }
    // register - create a new user, hash the password, issue access and refresh tokens
    public async Task<AuthResult> RegisterAsync(RegisterRequestDto request)
    {
        var emailExists = await _db.Users
            .AnyAsync(u => u.Email == request.Email);

        if (emailExists)
            return AuthResult.Fail(
                "An account with this email already exists.");

        var customerRole = await _db.Roles
            .FirstOrDefaultAsync(r => r.Name == "Customer");

        if (customerRole is null)
            return AuthResult.Fail(
                "Customer role is not configured.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            RoleId = customerRole.Id,
            Role = customerRole,

            // Attach the tracked Role so GenerateAccessToken
            // can read Role.Name.
            EmailConfirmed = false,

            // Real email verification comes in a later phase.
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);

        await _db.SaveChangesAsync();

        return await IssueTokensAsync(
            user,
            customerRole.Name
        );
    }

    // login - validate the email and password , issue access and refresh tokens
    public async Task<AuthResult> LoginAsync(LoginRequestDto request)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        // Deliberately vague error - never reveal
        // whether the email exists or not.
        if (user is null ||
            !_passwordHasher.VerifyPassword(
                user.PasswordHash,
                request.Password))
        {
            return AuthResult.Fail("Invalid email or password.");
        }

        if (!user.IsActive)
            return AuthResult.Fail(
                "This account has been deactivated.");

        return await IssueTokensAsync(
            user,
            user.Role.Name
        );
    }
    // Helper method to issue access and refresh tokens
    private async Task<AuthResult> IssueTokensAsync(
        User user,
        string roleName)
    {
        var accessToken =
            _jwtTokenService.GenerateAccessToken(user);

        var (plainRefreshToken, _) =
            await _refreshTokenService.CreateRefreshTokenAsync(
                user.Id);

        var response = new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = roleName,
            AccessToken = accessToken
        };

        return AuthResult.Ok(
            response,
            plainRefreshToken
        );
    }
    // refresh - validate the refresh token,issue a new access token and a new refresh token
    public async Task<AuthResult> RefreshAsync(string refreshToken)
    {
        var rotation = await _refreshTokenService
            .RotateRefreshTokenAsync(refreshToken);

        if (!rotation.Success)
        {
            return AuthResult.Fail(
                rotation.ReuseDetected
                    ? "Session invalidated due to suspicious activity. Please log in again."
                    : "Invalid or expired refresh token."
            );
        }

        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == rotation.UserId);

        if (user is null)
        {
            return AuthResult.Fail("User not found.");
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(user);

        var response = new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.Name,
            AccessToken = accessToken
        };

        return AuthResult.Ok(
            response,
            rotation.NewPlainToken!
        );
    }

    // logout - revoke the refresh token
    public async Task LogoutAsync(string refreshToken)
    {
        await _refreshTokenService
            .RevokeByPlainTokenAsync(refreshToken);
    }
    
    // Generates an email verification token for the user.
    public async Task<string?> RequestEmailVerificationAsync(string email)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || user.EmailConfirmed)
            return null;

        // Don't reveal whether the email exists or is already verified.
        return _jwtTokenService.GeneratePurposeToken(
            user.Id,
            "email-verification",
            60 * 24
        ); // Valid for 24 hours
    }

    // Verifies the email using the provided verification token.
    public async Task<bool> VerifyEmailAsync(string token)
    {
        var (isValid, userId) = _jwtTokenService
            .ValidatePurposeToken(token, "email-verification");

        if (!isValid)
            return false;

        var user = await _db.Users.FindAsync(userId);

        if (user is null)
            return false;

        user.EmailConfirmed = true;

        await _db.SaveChangesAsync();

        return true;
    }
    
    // Generates a password reset token for the user.
    public async Task<string?> RequestPasswordResetAsync(string email)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user is null)
            return null;

        // Caller returns generic success to prevent account enumeration.
        return _jwtTokenService.GeneratePurposeToken(
            user.Id,
            "password-reset",
            30
        ); // Valid for 30 minutes
    }

    // Resets the user's password and invalidates all active sessions.
    public async Task<bool> ResetPasswordAsync(string token,string newPassword)
    {
        var (isValid, userId) = _jwtTokenService
            .ValidatePurposeToken(token, "password-reset");

        if (!isValid)
            return false;

        var user = await _db.Users.FindAsync(userId);

        if (user is null)
            return false;

        user.PasswordHash = _passwordHasher.HashPassword(newPassword);

        // Password change invalidates all existing sessions.
        var activeTokens = await _db.RefreshTokens
            .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
            .ToListAsync();

        foreach (var tokenEntity in activeTokens)
        {
            tokenEntity.IsRevoked = true;
        }

        await _db.SaveChangesAsync();

        return true;
    }
}