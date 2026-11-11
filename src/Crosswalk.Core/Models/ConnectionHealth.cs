namespace Crosswalk.Core.Models;

public enum ConnectionHealth
{
    NotConnected,
    Healthy,
    Refreshing,
    NeedsReauthorization,
    SyncFailed
}

public sealed class ConnectionStatus
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public string? Color { get; init; }
    public required ConnectionHealth Health { get; set; }
    public DateTimeOffset? LastSuccessfulRefresh { get; set; }
    public int TicketCount { get; set; }
    public string? Error { get; set; }
    public string? IdentityDisplayName { get; set; }
    public string? IdentityAccountId { get; set; }
    public string? Email { get; init; }
    public required string BaseUrl { get; init; }
    public List<string> Projects { get; init; } = [];
    public List<BoardRef> Boards { get; init; } = [];
    public bool Enabled { get; init; }
}
