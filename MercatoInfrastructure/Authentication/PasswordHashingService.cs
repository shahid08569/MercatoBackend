using Microsoft.AspNetCore.Identity;
using MercatoApplication.Interfaces;
using MercatoDomain.Entities;

namespace MercatoInfrastructure.Authentication;
public class PasswordHashingService: IPasswordHashingService
{
    private readonly PasswordHasher<User> _hasher = new(); 
    public string HashPassword(string password)
    {
        // 'null!' because PasswordHasher needs a user object for its algorithm, // but doesn't actually read any of the user's data - it's just part of the API shape
      return _hasher.HashPassword(null!, password);
    } 
    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(null!, hashedPassword, providedPassword);
        return result == PasswordVerificationResult.Success;
    }
}
