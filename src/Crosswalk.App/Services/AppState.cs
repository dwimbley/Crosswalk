using Crosswalk.Core.Configuration;
using Crosswalk.Core.Demo;
using Crosswalk.Core.Models;
using Crosswalk.Core.Sync;

namespace Crosswalk.App.Services;

/// <summary>
/// Central application state shared across Blazor components.
/// Wraps either the real SyncService or the DemoSyncService.
/// </summary>
public sealed class AppState : IDisposable
{
    private readonly SyncService? _syncService;
    private readonly DemoSyncService? _demoService;

    public bool IsDemo => _demoService != null;
    public event Action? OnStateChanged;

    public AppState(SyncService syncService)
    {
        _syncService = syncService;
        _syncService.OnStateChanged += NotifyStateChanged;
    }

    public AppState(DemoSyncService demoService)
    {
        _demoService = demoService;
        _demoService.OnStateChanged += NotifyStateChanged;
    }

    public List<TicketViewModel> GetAllTickets()
        => _syncService?.GetAllTickets() ?? _demoService?.GetAllTickets() ?? [];

    public List<ConnectionStatus> GetConnectionStatuses()
        => _syncService?.GetConnectionStatusList() ?? _demoService?.GetConnectionStatusList() ?? [];

    public ConnectionStatus? GetConnectionStatus(string id)
        => GetConnectionStatuses().FirstOrDefault(c => c.Id == id);

    public async Task RefreshAllAsync(CancellationToken ct = default)
    {
        if (_syncService != null) await _syncService.RefreshAllAsync(ct);
        else if (_demoService != null) await _demoService.RefreshAllAsync(ct);
    }

    public async Task RefreshConnectionAsync(string connectionId, CancellationToken ct = default)
    {
        if (_syncService != null) await _syncService.RefreshConnectionAsync(connectionId, ct);
        else if (_demoService != null) await _demoService.RefreshConnectionAsync(connectionId, ct);
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_syncService != null) await _syncService.InitializeAsync(ct);
        // Demo is already initialized
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();

    public void Dispose()
    {
        if (_syncService != null) _syncService.OnStateChanged -= NotifyStateChanged;
        if (_demoService != null) _demoService.OnStateChanged -= NotifyStateChanged;
    }
}
