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
        // Log all CORS-related headers
        var origin = context.Request.Headers.Origin.FirstOrDefault();
        var userAgent = context.Request.Headers.UserAgent.FirstOrDefault();
        var referer = context.Request.Headers.Referer.FirstOrDefault();
        var host = context.Request.Headers.Host.FirstOrDefault();
        
        _logger.LogInformation("CORS Debug - Incoming Request:");
        _logger.LogInformation("  Method: {Method}", context.Request.Method);
        _logger.LogInformation("  Path: {Path}", context.Request.Path);
        _logger.LogInformation("  Origin: {Origin}", origin ?? "(null)");
        _logger.LogInformation("  Referer: {Referer}", referer ?? "(null)");
        _logger.LogInformation("  Host: {Host}", host ?? "(null)");
        _logger.LogInformation("  User-Agent: {UserAgent}", userAgent ?? "(null)");
        
        // Log if it's a preflight request
        if (context.Request.Method == "OPTIONS")
        {
            _logger.LogInformation("  >>> This is a PREFLIGHT request");
            var accessControlRequestMethod = context.Request.Headers["Access-Control-Request-Method"].FirstOrDefault();
            var accessControlRequestHeaders = context.Request.Headers["Access-Control-Request-Headers"].FirstOrDefault();
            _logger.LogInformation("  Access-Control-Request-Method: {Method}", accessControlRequestMethod ?? "(null)");
            _logger.LogInformation("  Access-Control-Request-Headers: {Headers}", accessControlRequestHeaders ?? "(null)");
        }

        await _next(context);

        // Log response headers
        _logger.LogInformation("CORS Debug - Response Headers:");
        _logger.LogInformation("  Status: {StatusCode}", context.Response.StatusCode);
        
        var responseHeaders = context.Response.Headers;
        if (responseHeaders.ContainsKey("Access-Control-Allow-Origin"))
        {
            _logger.LogInformation("  Access-Control-Allow-Origin: {Value}", responseHeaders["Access-Control-Allow-Origin"]);
        }
        else
        {
            _logger.LogWarning("  >>> Missing Access-Control-Allow-Origin header!");
        }
        
        if (responseHeaders.ContainsKey("Access-Control-Allow-Methods"))
        {
            _logger.LogInformation("  Access-Control-Allow-Methods: {Value}", responseHeaders["Access-Control-Allow-Methods"]);
        }
        
        if (responseHeaders.ContainsKey("Access-Control-Allow-Headers"))
        {
            _logger.LogInformation("  Access-Control-Allow-Headers: {Value}", responseHeaders["Access-Control-Allow-Headers"]);
        }
    }
}