using System;
using System.Collections.Generic;
using System.Text;

namespace MercatoApplication.Interfaces;
public interface IPasswordHashingService
{
    string HashPassword(string password);
    bool VerifyPassword(string hashedPassword, string providedPassword);
}
