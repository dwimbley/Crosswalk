using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Crosswalk.App.Services;

/// <summary>
/// Opens the default browser to the app URL on startup.
/// </summary>
public sealed class BrowserLauncher : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BrowserLauncher> _logger;

    public BrowserLauncher(IConfiguration configuration, ILogger<BrowserLauncher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var urls = _configuration["Urls"] ?? "http://127.0.0.1:5391";
        var url = urls.Split(';').First();

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start("open", url);
            else
                Process.Start("xdg-open", url);

            _logger.LogInformation("Opened browser to {Url}", url);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not open browser. Navigate to {Url} manually.", url);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
