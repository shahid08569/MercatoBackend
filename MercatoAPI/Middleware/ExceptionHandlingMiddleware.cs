using System.Net;
using System.Text.Json;
using MercatoApplication.Common;

namespace MercatoAPI.Middleware;
public class ExceptionHandlingMiddleware {
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    { 
        _next = next; 
        _logger = logger;
    }
    public async Task InvokeAsync(HttpContext context)
    { 
        try 
        { 
            // Let the request continue to the next step in the pipeline
            await _next(context);
        }
        catch (Exception ex) 
        { 
            // Log the FULL error details internally (for developers)
            _logger.LogError(ex, "Unhandled exception occurred"); 
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            // Send back only a SAFE, generic message (never expose stack traces to the client)
            var response = ApiResponse<object>.FailureResponse("An unexpected error occured. Please try again later");
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}