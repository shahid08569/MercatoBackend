namespace MercatoAPI.Middleware;
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;
    public SecurityHeadersMiddleware(
        RequestDelegate next,
        IWebHostEnvironment enviroment) {
        _next = next;
        _environment = enviroment;
    }
    public async Task InvokeAsync(HttpContext context)
    { 
        // Stops the browser from "guessing" content types (prevents some attacks)
        context.Response.Headers.Append(
            "X-Content-Type-Options",
            "nosniff");

        // Stops this API's responses from being embedded in an <iframe> on another site\
        context.Response.Headers.Append(
            "X-Frame-Options", 
            "DENY"); 

        // Tells browsers to only connect via HTTPS for the next 1 year
        context.Response.Headers.Append(
            "Strict-Transport-Security",
            "max-age=31536000; includeSubDomains");

        // Basic Content Security Policy - restricts where content can load from
        if (_environment.IsDevelopment())
        {
            // Development CSP:
            // Scalar uses inline scripts/styles and ASP.NET Core
            // development tools may use localhost WebSockets.
            context.Response.Headers.Append(
                "Content-Security-Policy",
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "connect-src 'self' http://localhost:* https://localhost:* ws://localhost:* wss://localhost:*; " +
                "img-src 'self' data: blob:; " +
                "font-src 'self' data: https:;");

        }
        else
        {
            // Production CSP:
            // Keep the policy significantly stricter.
            context.Response.Headers.Append(
                "Content-Security-Policy",
                "default-src 'self'; " +
                "script-src 'self'; " +
                "style-src 'self'; " +
                "img-src 'self' data:; " +
                "font-src 'self' data:;");
        }
        await _next(context);
    }
}