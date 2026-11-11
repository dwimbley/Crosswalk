using System.Text.Json;

namespace Crosswalk.Core.Cache;

public sealed class FileCacheStore : ICacheStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _cacheDir;

    public FileCacheStore(string configDirectory)
    {
        _cacheDir = Path.Combine(configDirectory, "cache");
        Directory.CreateDirectory(_cacheDir);
    }

    public async Task<CachedTicketSet?> LoadAsync(string connectionId, CancellationToken ct = default)
    {
        var path = GetPath(connectionId);
        if (!File.Exists(path))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(path, ct);
            return JsonSerializer.Deserialize<CachedTicketSet>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task SaveAsync(string connectionId, CachedTicketSet data, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_cacheDir);
        var path = GetPath(connectionId);
        var json = JsonSerializer.Serialize(data, JsonOptions);
        var tempPath = path + ".tmp";
        await File.WriteAllTextAsync(tempPath, json, ct);
        File.Move(tempPath, path, overwrite: true);
    }

    public Task DeleteAsync(string connectionId, CancellationToken ct = default)
    {
        var path = GetPath(connectionId);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    public Task ClearAllAsync(CancellationToken ct = default)
    {
        if (Directory.Exists(_cacheDir))
        {
            foreach (var file in Directory.GetFiles(_cacheDir, "*.json"))
                File.Delete(file);
        }
        return Task.CompletedTask;
    }

    private string GetPath(string connectionId)
        => Path.Combine(_cacheDir, $"{connectionId}.json");
}
