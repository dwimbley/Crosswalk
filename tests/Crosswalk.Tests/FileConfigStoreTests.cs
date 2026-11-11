using Crosswalk.Core.Configuration;

namespace Crosswalk.Tests;

public class FileConfigStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileConfigStore _store;

    public FileConfigStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "crosswalk-tests-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        _store = new FileConfigStore(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task Load_NoFile_ReturnsDefault()
    {
        var config = await _store.LoadAsync();

        Assert.Equal(1, config.Version);
        Assert.Equal(15, config.RefreshMinutes);
        Assert.Empty(config.Connections);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrips()
    {
        var config = new CrosswalkConfig
        {
            Version = 1,
            RefreshMinutes = 30,
            Connections =
            [
                new ConnectionConfig
                {
                    Id = "harborline",
                    DisplayName = "Harborline",
                    BaseUrl = "https://harborline.atlassian.net",
                    Email = "me@harborline.com",
                    Projects = ["CLM", "PLAT"],
                    Boards = [new BoardConfig { Id = 42, Name = "Claims Sprint Board" }],
                    StatusMap = new Dictionary<string, string> { { "Waiting", "blocked" } }
                }
            ]
        };

        await _store.SaveAsync(config);
        var loaded = await _store.LoadAsync();

        Assert.Equal(30, loaded.RefreshMinutes);
        Assert.Single(loaded.Connections);
        var conn = loaded.Connections[0];
        Assert.Equal("harborline", conn.Id);
        Assert.Equal("Harborline", conn.DisplayName);
        Assert.Equal(2, conn.Projects.Count);
        Assert.Single(conn.Boards);
        Assert.Equal("blocked", conn.StatusMap["Waiting"]);
    }

    [Fact]
    public async Task Save_IsAtomic_NoPartialWrites()
    {
        var config = new CrosswalkConfig { RefreshMinutes = 99 };
        await _store.SaveAsync(config);

        // The temp file should not exist after save
        Assert.False(File.Exists(_store.ConfigFilePath + ".tmp"));
        Assert.True(File.Exists(_store.ConfigFilePath));
    }

    [Fact]
    public async Task Load_CorruptedJson_ReturnsDefault()
    {
        Directory.CreateDirectory(_tempDir);
        await File.WriteAllTextAsync(_store.ConfigFilePath, "{ this is not valid json");

        var config = await _store.LoadAsync();

        Assert.Equal(1, config.Version);
        Assert.Empty(config.Connections);
    }

    [Fact]
    public void ConfigFilePath_IsCorrect()
    {
        Assert.Equal(Path.Combine(_tempDir, "config.json"), _store.ConfigFilePath);
    }

    [Fact]
    public void ConfigDirectory_IsCorrect()
    {
        Assert.Equal(_tempDir, _store.ConfigDirectory);
    }
}
