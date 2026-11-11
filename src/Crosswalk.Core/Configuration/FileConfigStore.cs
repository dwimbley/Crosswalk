using System.Text.Json;

namespace Crosswalk.Core.Configuration;

public sealed class FileConfigStore : IConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly SemaphoreSlim _lock = new(1, 1);

    public string ConfigDirectory { get; }
    public string ConfigFilePath { get; }

    public FileConfigStore(string? configDirectory = null)
    {
        ConfigDirectory = configDirectory ?? GetDefaultConfigDirectory();
        ConfigFilePath = Path.Combine(ConfigDirectory, "config.json");
    }

    public async Task<CrosswalkConfig> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(ConfigFilePath))
            return new CrosswalkConfig();

        await _lock.WaitAsync(ct);
        try
        {
            var json = await File.ReadAllTextAsync(ConfigFilePath, ct);
            var config = JsonSerializer.Deserialize<CrosswalkConfig>(json, JsonOptions);
            if (config is null)
                return new CrosswalkConfig();

            return Migrate(config);
        }
        catch (JsonException)
        {
            // Corrupted config — return default so the UI can surface the error
            return new CrosswalkConfig();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(CrosswalkConfig config, CancellationToken ct = default)
    {
        Directory.CreateDirectory(ConfigDirectory);

        await _lock.WaitAsync(ct);
        try
        {
            var json = JsonSerializer.Serialize(config, JsonOptions);
            var tempPath = ConfigFilePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, ct);
            File.Move(tempPath, ConfigFilePath, overwrite: true);
        }
        finally
        {
            _lock.Release();
        }
    }

    private static CrosswalkConfig Migrate(CrosswalkConfig config)
    {
        // Future migrations go here, keyed by config.Version
        // Example:
        // if (config.Version < 2) { /* migrate */ config.Version = 2; }
        return config;
    }

    private static string GetDefaultConfigDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Crosswalk");
        }

        // macOS and Linux: ~/.config/crosswalk
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".config", "crosswalk");
    }
}
