namespace Crosswalk.Core.Configuration;

public interface IConfigStore
{
    Task<CrosswalkConfig> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(CrosswalkConfig config, CancellationToken ct = default);
    string ConfigFilePath { get; }
    string ConfigDirectory { get; }
}
