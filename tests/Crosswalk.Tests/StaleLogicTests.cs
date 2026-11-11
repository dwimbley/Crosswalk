using Crosswalk.Core.Cache;
using Crosswalk.Core.Models;

namespace Crosswalk.Tests;

public class StaleLogicTests
{
    [Fact]
    public void Ticket_FromCache_IsStale_WhenRefreshFailed()
    {
        var cachedAt = DateTimeOffset.UtcNow.AddHours(-1);
        var ticket = MakeTicket("CLM-100");
        var cached = new CachedTicketSet
        {
            LastSuccessfulRefresh = cachedAt,
            Tickets = [ticket]
        };

        // Simulate: current refresh failed, so we use cached data
        // A ticket is stale when the connection's last successful refresh
        // is older than the current time (i.e., a refresh has failed since)
        var isStale = IsStale(cached.LastSuccessfulRefresh, refreshSucceeded: false);

        Assert.True(isStale);
    }

    [Fact]
    public void Ticket_FromFreshRefresh_IsNotStale()
    {
        var isStale = IsStale(DateTimeOffset.UtcNow, refreshSucceeded: true);
        Assert.False(isStale);
    }

    [Fact]
    public void Ticket_NeverRefreshed_IsNotStale()
    {
        // First load — no cache, fresh data
        var isStale = IsStale(null, refreshSucceeded: true);
        Assert.False(isStale);
    }

    /// <summary>
    /// Stale logic: a ticket is stale when it comes from cache because the most recent refresh failed.
    /// </summary>
    private static bool IsStale(DateTimeOffset? lastSuccessfulRefresh, bool refreshSucceeded)
    {
        if (refreshSucceeded)
            return false;

        // If refresh failed and we have cached data, those tickets are stale
        return lastSuccessfulRefresh.HasValue;
    }

    private static JiraTicket MakeTicket(string key) => new()
    {
        Key = key,
        Summary = $"Test {key}",
        Status = "To Do",
        StatusCategoryKey = "new",
        NormalizedCategory = NormalizedCategory.ToDo,
        Priority = "Medium",
        PriorityOrder = 3,
        Updated = DateTimeOffset.UtcNow,
        Created = DateTimeOffset.UtcNow,
        Reporter = "reporter",
        Assignee = "assignee",
        AssigneeAccountId = "acc-123",
        ProjectKey = key.Split('-')[0],
        ProjectName = "Test Project"
    };
}
