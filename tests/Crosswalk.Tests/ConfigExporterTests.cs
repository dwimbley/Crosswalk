using System.Text.Json;
using Crosswalk.Core.Configuration;

namespace Crosswalk.Tests;

public class ConfigExporterTests
{
    [Fact]
    public void Export_IncludesSecretRef_NotActualSecrets()
    {
        var config = new CrosswalkConfig
        {
            Connections =
            [
                new ConnectionConfig
                {
                    Id = "harborline",
                    DisplayName = "Harborline",
                    BaseUrl = "https://harborline.atlassian.net",
                    Email = "me@harborline.com"
                }
            ]
        };

        var json = ConfigExporter.Export(config);

        Assert.Contains("Crosswalk/harborline", json);
        // Should not contain any raw token values
        var doc = JsonDocument.Parse(json);
        var auth = doc.RootElement
            .GetProperty("connections")[0]
            .GetProperty("auth");
        Assert.Equal("apiToken", auth.GetProperty("type").GetString());
        Assert.Equal("Crosswalk/harborline", auth.GetProperty("secretRef").GetString());
    }

    [Fact]
    public void Import_SetsSecretRef()
    {
        var json = """
        {
            "version": 1,
            "refreshMinutes": 15,
            "connections": [{
                "id": "test",
                "displayName": "Test",
                "baseUrl": "https://test.atlassian.net",
                "email": "me@test.com",
                "auth": { "type": "apiToken" },
                "projects": [],
                "boards": []
            }]
        }
        """;

        var config = ConfigExporter.Import(json);

        Assert.Single(config.Connections);
        Assert.Equal("Crosswalk/test", config.Connections[0].Auth.SecretRef);
    }

    [Fact]
    public void Import_InvalidJson_Throws()
    {
        Assert.Throws<JsonException>(() => ConfigExporter.Import("not json"));
    }
}
