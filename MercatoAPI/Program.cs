//API documentation page
using MercatoInfrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Registers AppDbContext so it can be injected wherever needed.
// Tells EF Core to use SQL Server, with the connection string read from appsettings.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); //scalar - visual, browsable UI
}

app.UseHttpsRedirection();

//Add #2=Enabling CORS middleware
app.UseCors("AngularApp");

app.UseAuthorization();

app.MapControllers();

//Add #3= health check endpoint
app.MapGet("/api/health", () => Results.Ok(new {
    status="healthy",
    timestamp=DateTime.UtcNow
}));

app.MapGet("/api/health/db", async (AppDbContext db) =>
{
    bool canConnect = await db.Database.CanConnectAsync();
    return Results.Ok(new { databaseConnected = canConnect });
});

app.Run();
