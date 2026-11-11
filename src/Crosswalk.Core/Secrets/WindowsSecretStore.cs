using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace Crosswalk.Core.Secrets;

/// <summary>
/// Stores secrets using DPAPI (ProtectedData) on Windows.
/// Each secret is a file in %APPDATA%\Crosswalk\secrets\ encrypted with CurrentUser scope.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsSecretStore : ISecretStore
{
    private readonly string _secretsDir;

    public string StoreName => "Windows DPAPI (encrypted, per-user)";

    public WindowsSecretStore(string? baseDir = null)
    {
        var appData = baseDir
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Crosswalk");
        _secretsDir = Path.Combine(appData, "secrets");
        Directory.CreateDirectory(_secretsDir);
    }

    public Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        var path = GetPath(key);
        if (!File.Exists(path))
            return Task.FromResult<string?>(null);

        var encrypted = File.ReadAllBytes(path);
        var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
        return Task.FromResult<string?>(Encoding.UTF8.GetString(decrypted));
    }

    public Task SetAsync(string key, string value, CancellationToken ct = default)
    {
        var path = GetPath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var plainBytes = Encoding.UTF8.GetBytes(value);
        var encrypted = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
        var tempPath = path + ".tmp";
        File.WriteAllBytes(tempPath, encrypted);
        File.Move(tempPath, path, overwrite: true);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var path = GetPath(key);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    private string GetPath(string key)
    {
        // key is like "Crosswalk/harborline" → file "harborline.secret"
        var safeName = key.Replace('/', '_').Replace('\\', '_');
        return Path.Combine(_secretsDir, safeName + ".secret");
    }
}
