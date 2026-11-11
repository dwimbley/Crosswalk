using System.Text.Json.Serialization;

namespace Crosswalk.Core.Configuration;

public sealed class CrosswalkConfig
{
    public int Version { get; set; } = 1;
    public int RefreshMinutes { get; set; } = 15;
    public QueueDefaults Defaults { get; set; } = new();
    public List<ConnectionConfig> Connections { get; set; } = [];
}

public sealed class QueueDefaults
{
    public string View { get; set; } = "open";
    public string Sort { get; set; } = "priority";
    public string GroupBy { get; set; } = "none";
    public bool IncludeCompleted { get; set; }
    public string Density { get; set; } = "compact";
}

public sealed class ConnectionConfig
{
    public required string Id { get; set; }
    public required string DisplayName { get; set; }
    public required string BaseUrl { get; set; }
    public required string Email { get; set; }
    public AuthConfig Auth { get; set; } = new();
    public List<string> Projects { get; set; } = [];
    public List<BoardConfig> Boards { get; set; } = [];
    public string? ExtraJql { get; set; }
    public Dictionary<string, string> StatusMap { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? Color { get; set; }
    public bool Enabled { get; set; } = true;
}

public sealed class AuthConfig
{
    public string Type { get; set; } = "apiToken";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SecretRef { get; set; }
}

public sealed class BoardConfig
{
    public required int Id { get; set; }
    public required string Name { get; set; }
}
