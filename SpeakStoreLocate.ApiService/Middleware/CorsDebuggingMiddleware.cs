namespace SpeakStoreLocate.ApiService.Middleware;

public class CorsDebuggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorsDebuggingMiddleware> _logger;

    public CorsDebuggingMiddleware(RequestDelegate next, ILogger<CorsDebuggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_logger.IsEnabled(LogLevel.Debug))
        {
            await _next(context);
            return;
        }

        // Log all CORS-related headers
        var origin = context.Request.Headers.Origin.FirstOrDefault();
        var userAgent = context.Request.Headers.UserAgent.FirstOrDefault();
        var referer = context.Request.Headers.Referer.FirstOrDefault();
        var host = context.Request.Headers.Host.FirstOrDefault();
        
        _logger.LogDebug("CORS Debug - Incoming Request:");
        _logger.LogDebug("  Method: {Method}", context.Request.Method);
        _logger.LogDebug("  Path: {Path}", context.Request.Path);
        _logger.LogDebug("  Origin: {Origin}", origin ?? "(null)");
        _logger.LogDebug("  Referer: {Referer}", referer ?? "(null)");
        _logger.LogDebug("  Host: {Host}", host ?? "(null)");
        _logger.LogDebug("  User-Agent: {UserAgent}", userAgent ?? "(null)");
        
        // Log if it's a preflight request
        if (context.Request.Method == "OPTIONS")
        {
            _logger.LogDebug("  >>> This is a PREFLIGHT request");
            var accessControlRequestMethod = context.Request.Headers["Access-Control-Request-Method"].FirstOrDefault();
            var accessControlRequestHeaders = context.Request.Headers["Access-Control-Request-Headers"].FirstOrDefault();
            _logger.LogDebug("  Access-Control-Request-Method: {Method}", accessControlRequestMethod ?? "(null)");
            _logger.LogDebug("  Access-Control-Request-Headers: {Headers}", accessControlRequestHeaders ?? "(null)");
        }

        await _next(context);

        // Log response headers
        _logger.LogDebug("CORS Debug - Response Headers:");
        _logger.LogDebug("  Status: {StatusCode}", context.Response.StatusCode);
        
        var responseHeaders = context.Response.Headers;
        if (responseHeaders.ContainsKey("Access-Control-Allow-Origin"))
        {
            _logger.LogDebug("  Access-Control-Allow-Origin: {Value}", responseHeaders["Access-Control-Allow-Origin"]);
        }
        else
        {
            _logger.LogWarning("  >>> Missing Access-Control-Allow-Origin header!");
        }
        
        if (responseHeaders.ContainsKey("Access-Control-Allow-Methods"))
        {
            _logger.LogDebug("  Access-Control-Allow-Methods: {Value}", responseHeaders["Access-Control-Allow-Methods"]);
        }
        
        if (responseHeaders.ContainsKey("Access-Control-Allow-Headers"))
        {
            _logger.LogDebug("  Access-Control-Allow-Headers: {Value}", responseHeaders["Access-Control-Allow-Headers"]);
        }
    }
}