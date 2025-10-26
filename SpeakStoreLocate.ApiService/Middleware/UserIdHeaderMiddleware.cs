using System.Text.RegularExpressions;

namespace SpeakStoreLocate.ApiService.Middleware;

public class UserIdHeaderMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserIdHeaderMiddleware> _logger;
    private static readonly Regex Allowed = new("^[A-Za-z0-9_-]{1,64}$", RegexOptions.Compiled);

    public UserIdHeaderMiddleware(RequestDelegate next, ILogger<UserIdHeaderMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUserContext userContext)
    {
        // Preflight-Requests ohne Prüfung durchlassen
        if (HttpMethods.IsOptions(context.Request.Method))
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
}
