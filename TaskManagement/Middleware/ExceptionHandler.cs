using Microsoft.AspNetCore.Diagnostics;
using System.ComponentModel.DataAnnotations;
using TaskManagement.Common;

namespace TaskManagement.Middleware;

public static class ExceptionHandler
{
    public static void ConfigureExceptionHandler(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(appError =>
        {
            appError.Run(static async context =>
            {
                var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                if (contextFeature?.Error == null) return;

                var exception = contextFeature.Error;
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

                logger.LogError(
                    exception,
                    "Exception occurred. Path: {Path}, Method: {Method}",
                    context.Request.Path,
                    context.Request.Method
                );

                var (statusCode, title, detail) = exception switch
                {
                    ValidationException => (400, "Validation Error", exception.Message),
                    NotFoundException => (404, "Not Found", exception.Message),
                    ConflictException => (409, "Conflict", exception.Message),
                    _ => (500, "Internal Server Error", "An unexpected error occurred")
                };

                var problemDetails = new
                {
                    type = GetProblemTypeUrl(statusCode),
                    title,
                    status = statusCode,
                    detail,
                    instance = context.Request.Path.Value
                };

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsJsonAsync(problemDetails);
            });
        });
    }

    private static string GetProblemTypeUrl(int statusCode) => statusCode switch
    {
        400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        409 => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
        _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
    };
}
