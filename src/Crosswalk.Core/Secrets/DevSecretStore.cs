using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Crosswalk.Core.Secrets;

/// <summary>
/// Plaintext JSON file secret store for development only.
/// Enabled only when CROSSWALK_DEV_SECRETS=true.
/// </summary>
public sealed class DevSecretStore : ISecretStore
{
    private readonly string _filePath;
    private readonly ILogger<DevSecretStore> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public string StoreName => "Development (plaintext, NOT SECURE)";

    public DevSecretStore(string configDirectory, ILogger<DevSecretStore> logger)
    {
        _filePath = Path.Combine(configDirectory, "dev-secrets.json");
        _logger = logger;
        _logger.LogWarning("⚠ Using plaintext dev secret store at {Path}. Do NOT use in production.", _filePath);
    }

    public static bool IsEnabled =>
        string.Equals(Environment.GetEnvironmentVariable("CROSSWALK_DEV_SECRETS"), "true", StringComparison.OrdinalIgnoreCase);

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        var store = await LoadStoreAsync(ct);
        return store.TryGetValue(key, out var value) ? value : null;
    }

    public async Task SetAsync(string key, string value, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var store = await LoadStoreAsync(ct);
            store[key] = value;
            await SaveStoreAsync(store, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var store = await LoadStoreAsync(ct);
            if (store.Remove(key))
                await SaveStoreAsync(store, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<Dictionary<string, string>> LoadStoreAsync(CancellationToken ct)
    {
        if (!File.Exists(_filePath))
            return new Dictionary<string, string>();

        var json = await File.ReadAllTextAsync(_filePath, ct);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
    }

    private async Task SaveStoreAsync(Dictionary<string, string> store, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var json = JsonSerializer.Serialize(store, new JsonSerializerOptions { WriteIndented = true });
        var tempPath = _filePath + ".tmp";
        await File.WriteAllTextAsync(tempPath, json, ct);
        File.Move(tempPath, _filePath, overwrite: true);
    }
}
