using Crosswalk.Core.Models;

namespace Crosswalk.Core.Demo;

/// <summary>
/// A sync service substitute for demo mode. Serves fixture data with no Jira calls.
/// </summary>
public sealed class DemoSyncService
{
    private readonly Dictionary<string, ConnectionStatus> _statuses;
    private readonly Dictionary<string, List<TicketViewModel>> _tickets;

    public event Action? OnStateChanged;

    public DemoSyncService()
    {
        _statuses = DemoDataProvider.GetDemoStatuses();
        _tickets = DemoDataProvider.GetDemoTickets();
    }

    public IReadOnlyDictionary<string, ConnectionStatus> ConnectionStatuses => _statuses;

    public List<TicketViewModel> GetAllTickets()
        => _tickets.Values.SelectMany(t => t).ToList();

    public List<ConnectionStatus> GetConnectionStatusList()
        => [.. _statuses.Values];

    public Task RefreshAllAsync(CancellationToken ct = default)
    {
        // In demo mode, simulate a refresh by updating timestamps
        foreach (var status in _statuses.Values)
        {
            if (status.Health == ConnectionHealth.Healthy)
                status.LastSuccessfulRefresh = DateTimeOffset.Now;
        }
        OnStateChanged?.Invoke();
        return Task.CompletedTask;
    }

    public Task RefreshConnectionAsync(string connectionId, CancellationToken ct = default)
    {
        if (_statuses.TryGetValue(connectionId, out var status) && status.Health == ConnectionHealth.Healthy)
            status.LastSuccessfulRefresh = DateTimeOffset.Now;
        OnStateChanged?.Invoke();
        return Task.CompletedTask;
    }
}
