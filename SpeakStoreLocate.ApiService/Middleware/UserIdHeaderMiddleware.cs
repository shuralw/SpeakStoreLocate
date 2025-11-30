using System.Text.RegularExpressions;

namespace SpeakStoreLocate.ApiService.Middleware;

public class UserIdHeaderMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserIdHeaderMiddleware> _logger;
    private static readonly Regex Allowed = new("^[A-Za-z0-9_-]{1,64}$", RegexOptions.Compiled);

    // Pfade, die von der X-User-Id-Prüfung ausgenommen werden sollen
    private static readonly PathString[] ExcludedPaths =
    [
        new("/health"),           // Health Checks
        new("/ready"),            // evtl. weiterer Probe-Endpunkt
        new("/liveness"),         // evtl. weiterer Probe-Endpunkt
        new("/metrics")           // z.B. für Monitoring
    ];

    public UserIdHeaderMiddleware(RequestDelegate next, ILogger<UserIdHeaderMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUserContext userContext)
    {
        // Optionen/Preflight-Requests ohne Prüfung durchlassen
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // Bestimmte Pfade (Health Checks, Monitoring, Aspire-Infra) von der Prüfung ausnehmen
        if (IsExcludedPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var userId = context.Request.Headers["X-User-Id"].FirstOrDefault()?.Trim();

        if (string.IsNullOrWhiteSpace(userId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Missing X-User-Id header");
            return;
        }

        if (!Allowed.IsMatch(userId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Invalid X-User-Id header");
            return;
        }

        userContext.UserId = userId;
        using (_logger.BeginScope(new Dictionary<string, object> { ["UserId"] = userId }))
        {
            await _next(context);
        }
    }

    private static bool IsExcludedPath(PathString requestPath)
    {
        foreach (var excluded in ExcludedPaths)
        {
            if (requestPath.StartsWithSegments(excluded, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
