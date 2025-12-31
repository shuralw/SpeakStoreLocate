using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SpeakStoreLocate.ApiService.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception. TraceIdentifier={TraceIdentifier} TraceId={TraceId} Path={Path} Method={Method}",
                context.TraceIdentifier,
                Activity.Current?.TraceId.ToString(),
                context.Request.Path.ToString(),
                context.Request.Method);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = _env.IsDevelopment() ? ex.ToString() : "An unexpected error occurred.",
                Instance = context.TraceIdentifier
            };

            // Helpful for clients when reporting issues
            var traceId = Activity.Current?.TraceId.ToString();
            if (!string.IsNullOrWhiteSpace(traceId))
            {
                context.Response.Headers["X-Trace-Id"] = traceId;
            }

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
