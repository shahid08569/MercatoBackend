namespace MercatoAPI.Middleware;
public static class MiddlewareExtensions 
{
    // Handles unhandled exceptions globally and returns consistent API responses
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app) 
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>(); 
    }
    // Adds security-related HTTP headers to application responses
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) 
    { 
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}