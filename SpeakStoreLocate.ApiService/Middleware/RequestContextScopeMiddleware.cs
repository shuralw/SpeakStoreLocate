using System.Diagnostics;

namespace SpeakStoreLocate.ApiService.Middleware;

public sealed class RequestContextScopeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestContextScopeMiddleware> _logger;

    public RequestContextScopeMiddleware(RequestDelegate next, ILogger<RequestContextScopeMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = Activity.Current?.TraceId.ToString();
        if (!string.IsNullOrWhiteSpace(traceId))
        {
            context.Response.Headers["X-Trace-Id"] = traceId;
        }

        var scope = new Dictionary<string, object?>
        {
            ["TraceIdentifier"] = context.TraceIdentifier,
            ["TraceId"] = traceId,
            ["SpanId"] = Activity.Current?.SpanId.ToString(),
            ["RequestMethod"] = context.Request.Method,
            ["RequestPath"] = context.Request.Path.ToString(),
            ["RemoteIp"] = context.Connection.RemoteIpAddress?.ToString(),
            ["Origin"] = context.Request.Headers.Origin.ToString(),
        };

        using (_logger.BeginScope(scope))
        {
            await _next(context);
        }
    }
}
