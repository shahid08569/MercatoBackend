//API documentation page
using MercatoAPI.Middleware;
using MercatoApplication.Common;
using MercatoInfrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Microsoft.AspNetCore.RateLimiting;
using Asp.Versioning;
using MercatoInfrastructure.Authentication;
using MercatoApplication.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

//Serilog Logging configuration
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        "Logs/log-.text",
        rollingInterval: RollingInterval.Day
    )
    .CreateLogger();
//creating builder
var builder = WebApplication.CreateBuilder(args);

//AddRateLimiter service register
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);//per minute
        limiterOptions.QueueLimit = 0;//extra requests turant reject
    });
    options.RejectionStatusCode = 429;//to many request  ka hhtp status code
});
//Change the default log from .net own to the Serilog log
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();

//Adding Api Versioning Service
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Registers AppDbContext so it can be injected wherever needed.
// Tells EF Core to use SQL Server, with the connection string read from appsettings.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
    );

// Binds the "JwtSettings" section from appsettings.json + User Secrets into the JwtSettings class
builder.Services.Configure<JwtSettings>
    (
    builder.Configuration.GetSection("JwtSettings")
    );
builder.Services.AddScoped<IPasswordHashingService, PasswordHashingService>(); 
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => {
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidIssuer = jwtSettings.Issuer,
    ValidateAudience = true,
    ValidAudience = jwtSettings.Audience,
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
    ValidateLifetime = true,
    ClockSkew = TimeSpan.Zero
   // no extra grace period after expiry
 };
 });
//dependency injection for password hashing service
builder.Services.AddScoped<IPasswordHashingService, PasswordHashingService>();

//Add #1 = Registering CORS service

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();
//Middleware for global exception handling
app.UseGlobalExceptionHandling();
//Adding security headers
app.UseSecurityHeaders();
// add rules if rule table is empty
using(var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!db.Roles.Any())
    {
        db.Roles.AddRange(
            new MercatoDomain.Entities.Role { Name = "Customer" },
            new MercatoDomain.Entities.Role { Name = "Admin" },
            new MercatoDomain.Entities.Role { Name = "SuperAdmin" }
            );
        db.SaveChanges();
    }
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); //scalar - visual, browsable UI 
}

app.UseHttpsRedirection();

//Add #2=Enabling CORS middleware
app.UseCors("AngularApp");

//enabling middleware pipeline
app.UseRateLimiter();
//Authenticate and authorization middleware for JWT token validation
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

//Add #3= health check endpoint
app.MapGet("/api/v1/health", () =>
{
    var response = new
    {
        status = "healthy",
        timestamp = DateTime.UtcNow
    };

    return Results.Ok(
        ApiResponse<object>.SuccessResponse(response)
    );
});
//.RequireRateLimiting("fixed");
//Add db health check point
app.MapGet("/api/v1/health/db", async (AppDbContext db) =>
{
    bool canConnect = await db.Database.CanConnectAsync();

    return Results.Ok(new
    {
        databaseConnected = canConnect
    });
});

app.Run();
