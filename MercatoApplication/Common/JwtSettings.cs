using System;
using System.Collections.Generic;
using System.Text;

namespace MercatoApplication.Common;
// Maps to the "JwtSettings" section in appsettings.json
public class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; }
    public int RefreshTokenExpirationDays { get; set; }
    public string SecretKey { get; set; } = string.Empty;
}
