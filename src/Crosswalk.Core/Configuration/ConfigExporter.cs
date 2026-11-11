using System.Text.Json;

namespace Crosswalk.Core.Configuration;

/// <summary>
/// Exports and imports config without secrets. On import, SecretRef values are preserved
/// but the actual tokens must be re-entered by the user.
/// </summary>
public static class ConfigExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static string Export(CrosswalkConfig config)
    {
        // Deep-clone to strip any in-memory-only fields
        var exportable = JsonSerializer.Deserialize<CrosswalkConfig>(
            JsonSerializer.Serialize(config, JsonOptions), JsonOptions)!;

        // Ensure secret refs are present but no actual secret values leak
        foreach (var conn in exportable.Connections)
        {
            conn.Auth.SecretRef = $"Crosswalk/{conn.Id}";
        }

        return JsonSerializer.Serialize(exportable, JsonOptions);
    }

    public static CrosswalkConfig Import(string json)
    {
        var config = JsonSerializer.Deserialize<CrosswalkConfig>(json, JsonOptions)
            ?? throw new InvalidOperationException("Invalid config JSON.");

        // Validate basic structure
        if (config.Version < 1)
            throw new InvalidOperationException("Unsupported config version.");

        // Normalize secret refs
        foreach (var conn in config.Connections)
        {
            conn.Auth.SecretRef = $"Crosswalk/{conn.Id}";
        }

        return config;
    }
}
