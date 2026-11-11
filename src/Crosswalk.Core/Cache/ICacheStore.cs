using Crosswalk.Core.Models;

namespace Crosswalk.Core.Cache;

public interface ICacheStore
{
    Task<CachedTicketSet?> LoadAsync(string connectionId, CancellationToken ct = default);
    Task SaveAsync(string connectionId, CachedTicketSet data, CancellationToken ct = default);
    Task DeleteAsync(string connectionId, CancellationToken ct = default);
    Task ClearAllAsync(CancellationToken ct = default);
}

public sealed class CachedTicketSet
{
    public required DateTimeOffset LastSuccessfulRefresh { get; init; }
    public required List<JiraTicket> Tickets { get; init; }
}
