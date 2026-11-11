using System.Net;

namespace Crosswalk.App.Security;

/// <summary>
/// Rejects requests from non-loopback addresses and validates Origin header
/// to prevent CSRF/cross-origin attacks against the local API.
/// </summary>
public sealed class LocalOnlyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LocalOnlyMiddleware> _logger;

    public LocalOnlyMiddleware(RequestDelegate next, ILogger<LocalOnlyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress;

        // Only allow loopback connections
        if (remoteIp != null && !IPAddress.IsLoopback(remoteIp))
        {
            _logger.LogWarning("Rejected non-loopback request from {RemoteIp}", remoteIp);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Crosswalk only accepts connections from localhost.");
            return;
        }

        // For API endpoints, validate Origin header
        if (context.Request.Path.StartsWithSegments("/api") &&
            context.Request.Headers.TryGetValue("Origin", out var origin))
        {
            var allowed = GetAllowedOrigins(context);
            if (!allowed.Any(o => string.Equals(o, origin.ToString(), StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Rejected cross-origin API request from {Origin}", origin.ToString());
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Cross-origin requests are not allowed.");
                return;
            }
        }

        await _next(context);
    }

    private static string[] GetAllowedOrigins(HttpContext context)
    {
        var host = context.Request.Host;
        return
        [
            $"http://127.0.0.1:{host.Port}",
            $"http://localhost:{host.Port}",
            $"https://127.0.0.1:{host.Port}",
            $"https://localhost:{host.Port}"
        ];
    }
}
