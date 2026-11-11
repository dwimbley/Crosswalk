using Crosswalk.Core.Configuration;

namespace Crosswalk.App.Services;

/// <summary>
/// Background service that runs scheduled refreshes at the configured interval.
/// </summary>
public sealed class BackgroundRefreshService : BackgroundService
{
    private readonly AppState _appState;
    private readonly IConfigStore _configStore;
    private readonly ILogger<BackgroundRefreshService> _logger;

    public BackgroundRefreshService(AppState appState, IConfigStore configStore, ILogger<BackgroundRefreshService> logger)
    {
        _appState = appState;
        _configStore = configStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial load
        try
        {
            await _appState.InitializeAsync(stoppingToken);
            await _appState.RefreshAllAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initial refresh failed");
        }

        // Periodic refresh
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var config = await _configStore.LoadAsync(stoppingToken);
                var interval = TimeSpan.FromMinutes(Math.Max(config.RefreshMinutes, 1));
                await Task.Delay(interval, stoppingToken);
                await _appState.RefreshAllAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled refresh failed");
            }
        }
    }
}
