using Crosswalk.Core.Cache;
using Crosswalk.Core.Configuration;
using Crosswalk.Core.Jira;
using Crosswalk.Core.Models;
using Crosswalk.Core.Secrets;
using Microsoft.Extensions.Logging;

namespace Crosswalk.Core.Sync;

public sealed class SyncService
{
    private readonly IJiraClient _jiraClient;
    private readonly IConfigStore _configStore;
    private readonly ISecretStore _secretStore;
    private readonly ICacheStore _cacheStore;
    private readonly ILogger<SyncService> _logger;

    private readonly Dictionary<string, ConnectionStatus> _connectionStatuses = new();
    private readonly Dictionary<string, List<TicketViewModel>> _ticketsByConnection = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public event Action? OnStateChanged;

    public SyncService(
        IJiraClient jiraClient,
        IConfigStore configStore,
        ISecretStore secretStore,
        ICacheStore cacheStore,
        ILogger<SyncService> logger)
    {
        _jiraClient = jiraClient;
        _configStore = configStore;
        _secretStore = secretStore;
        _cacheStore = cacheStore;
        _logger = logger;
    }

    public IReadOnlyDictionary<string, ConnectionStatus> ConnectionStatuses => _connectionStatuses;

    public List<TicketViewModel> GetAllTickets()
    {
        lock (_ticketsByConnection)
        {
            return _ticketsByConnection.Values.SelectMany(t => t).ToList();
        }
    }

    public List<ConnectionStatus> GetConnectionStatusList()
    {
        lock (_connectionStatuses)
        {
            return [.. _connectionStatuses.Values];
        }
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var config = await _configStore.LoadAsync(ct);
        foreach (var conn in config.Connections.Where(c => c.Enabled))
        {
            _connectionStatuses[conn.Id] = CreateStatus(conn, ConnectionHealth.NotConnected);

            // Load cached tickets if available
            var cached = await _cacheStore.LoadAsync(conn.Id, ct);
            if (cached != null)
            {
                var vms = MapToViewModels(cached.Tickets, conn, isStale: true, cached.LastSuccessfulRefresh);
                lock (_ticketsByConnection)
                {
                    _ticketsByConnection[conn.Id] = vms;
                }
                _connectionStatuses[conn.Id].TicketCount = cached.Tickets.Count;
                _connectionStatuses[conn.Id].LastSuccessfulRefresh = cached.LastSuccessfulRefresh;
                _connectionStatuses[conn.Id].Health = ConnectionHealth.Healthy;
            }
        }
        NotifyStateChanged();
    }

    public async Task RefreshAllAsync(CancellationToken ct = default)
    {
        var config = await _configStore.LoadAsync(ct);
        var tasks = config.Connections
            .Where(c => c.Enabled)
            .Select(c => RefreshConnectionAsync(c, ct));

        await Task.WhenAll(tasks);
    }

    public async Task RefreshConnectionAsync(string connectionId, CancellationToken ct = default)
    {
        var config = await _configStore.LoadAsync(ct);
        var conn = config.Connections.FirstOrDefault(c => c.Id == connectionId);
        if (conn == null) return;

        await RefreshConnectionAsync(conn, ct);
    }

    private async Task RefreshConnectionAsync(ConnectionConfig conn, CancellationToken ct)
    {
        var status = _connectionStatuses.GetValueOrDefault(conn.Id) ?? CreateStatus(conn, ConnectionHealth.NotConnected);
        _connectionStatuses[conn.Id] = status;

        status.Health = ConnectionHealth.Refreshing;
        NotifyStateChanged();

        try
        {
            var apiToken = await _secretStore.GetAsync($"Crosswalk/{conn.Id}", ct);
            if (string.IsNullOrEmpty(apiToken))
            {
                status.Health = ConnectionHealth.NeedsReauthorization;
                status.Error = "No API token found. Please re-enter your token.";
                NotifyStateChanged();
                return;
            }

            // Verify identity
            var user = await _jiraClient.GetMyselfAsync(conn.BaseUrl, conn.Email, apiToken, ct);
            status.IdentityDisplayName = user.DisplayName;
            status.IdentityAccountId = user.AccountId;

            // Build JQL
            var jql = BuildJql(conn);
            _logger.LogInformation("Refreshing {ConnectionId} with JQL: {Jql}", conn.Id, jql);

            // Search tickets
            var tickets = await _jiraClient.SearchTicketsAsync(conn.BaseUrl, conn.Email, apiToken, jql, ct);

            // Apply per-connection status map
            foreach (var ticket in tickets)
            {
                ticket.NormalizedCategory = StatusNormalizer.Normalize(
                    ticket.Status, ticket.StatusCategoryKey, conn.StatusMap);
            }

            // Deduplicate within site (same key from overlapping board queries)
            tickets = TicketMerger.DeduplicateWithinSite(tickets);

            // Determine board membership
            if (conn.Boards.Count > 0)
            {
                var issueKeys = tickets.Select(t => t.Key).ToList();
                foreach (var board in conn.Boards)
                {
                    try
                    {
                        var boardKeys = await _jiraClient.GetBoardIssueKeysAsync(
                            conn.BaseUrl, conn.Email, apiToken, board.Id, issueKeys, ct);

                        foreach (var key in boardKeys)
                        {
                            var ticket = tickets.FirstOrDefault(t => t.Key == key);
                            if (ticket != null && !ticket.Boards.Any(b => b.Id == board.Id))
                            {
                                ticket.Boards.Add(new BoardRef { Id = board.Id, Name = board.Name });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get board {BoardId} issues for {ConnectionId}", board.Id, conn.Id);
                    }
                }
            }

            // Cache
            var now = DateTimeOffset.UtcNow;
            await _cacheStore.SaveAsync(conn.Id, new CachedTicketSet
            {
                LastSuccessfulRefresh = now,
                Tickets = tickets
            }, ct);

            // Update state
            var vms = MapToViewModels(tickets, conn, isStale: false, now);
            lock (_ticketsByConnection)
            {
                _ticketsByConnection[conn.Id] = vms;
            }

            status.Health = ConnectionHealth.Healthy;
            status.LastSuccessfulRefresh = now;
            status.TicketCount = tickets.Count;
            status.Error = null;
        }
        catch (JiraAuthException ex)
        {
            _logger.LogWarning(ex, "Auth failure for {ConnectionId}", conn.Id);
            status.Health = ConnectionHealth.NeedsReauthorization;
            status.Error = ex.Message;
        }
        catch (JiraRateLimitException ex)
        {
            _logger.LogWarning(ex, "Rate limited for {ConnectionId}, retry after {RetryAfter}", conn.Id, ex.RetryAfter);
            status.Health = ConnectionHealth.SyncFailed;
            status.Error = $"Rate limited. Retry after {ex.RetryAfter.TotalSeconds:0}s.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed for {ConnectionId}", conn.Id);
            status.Health = ConnectionHealth.SyncFailed;
            status.Error = ex.Message;
        }

        NotifyStateChanged();
    }

    public static string BuildJql(ConnectionConfig conn)
    {
        var parts = new List<string> { "assignee = currentUser()", "status != Done",
            "status != \"Approved By Customer\"",
            "status != \"Pending Production Deployment\"", "status != \"Waiting for customer\"" };

        if (conn.Projects.Count > 0)
        {
            var projects = string.Join(", ", conn.Projects.Select(p => $"\"{p}\""));
            parts.Add($"project in ({projects})");
        }

        if (!string.IsNullOrWhiteSpace(conn.ExtraJql))
            parts.Add($"({conn.ExtraJql.Trim()})");

        var jql = string.Join(" AND ", parts);
        jql += " ORDER BY priority ASC, updated DESC";
        return jql;
    }

    private static List<TicketViewModel> MapToViewModels(
        List<JiraTicket> tickets,
        ConnectionConfig conn,
        bool isStale,
        DateTimeOffset? lastRefresh)
    {
        return tickets.Select(t => new TicketViewModel
        {
            Key = t.Key,
            Summary = t.Summary,
            Status = t.Status,
            StatusCategoryKey = t.StatusCategoryKey,
            NormalizedCategory = t.NormalizedCategory,
            Priority = t.Priority,
            PriorityOrder = t.PriorityOrder,
            DueDate = t.DueDate,
            Updated = t.Updated,
            Created = t.Created,
            Reporter = t.Reporter,
            Assignee = t.Assignee,
            AssigneeAccountId = t.AssigneeAccountId,
            ProjectKey = t.ProjectKey,
            ProjectName = t.ProjectName,
            DescriptionHtml = t.DescriptionHtml,
            Boards = t.Boards,
            SiteId = conn.Id,
            SiteDisplayName = conn.DisplayName,
            SiteColor = conn.Color,
            SiteBaseUrl = conn.BaseUrl,
            SiteEmail = conn.Email,
            JiraUrl = JiraUrlNormalizer.BuildTicketUrl(conn.BaseUrl, t.Key),
            IsStale = isStale,
            LastSuccessfulRefresh = lastRefresh
        }).ToList();
    }

    private static ConnectionStatus CreateStatus(ConnectionConfig conn, ConnectionHealth health) => new()
    {
        Id = conn.Id,
        DisplayName = conn.DisplayName,
        Color = conn.Color,
        Health = health,
        Email = conn.Email,
        BaseUrl = conn.BaseUrl,
        Projects = [.. conn.Projects],
        Boards = conn.Boards.Select(b => new BoardRef { Id = b.Id, Name = b.Name }).ToList(),
        Enabled = conn.Enabled
    };

    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}
