using Crosswalk.Core.Configuration;

namespace Crosswalk.App.Endpoints;

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/settings", async (IConfigStore configStore) =>
        {
            var config = await configStore.LoadAsync();
            return Results.Ok(new SettingsDto
            {
                RefreshMinutes = config.RefreshMinutes,
                Defaults = config.Defaults,
                ConfigFilePath = configStore.ConfigFilePath
            });
        });

        app.MapPut("/api/settings", async (SettingsDto settings, IConfigStore configStore) =>
        {
            var config = await configStore.LoadAsync();
            config.RefreshMinutes = settings.RefreshMinutes;
            config.Defaults = settings.Defaults;
            await configStore.SaveAsync(config);
            return Results.Ok(settings);
        });

        app.MapGet("/api/config/export", async (IConfigStore configStore) =>
        {
            var config = await configStore.LoadAsync();
            var json = ConfigExporter.Export(config);
            return Results.Text(json, "application/json");
        });

        app.MapPost("/api/config/import", async (HttpContext ctx, IConfigStore configStore) =>
        {
            using var reader = new StreamReader(ctx.Request.Body);
            var json = await reader.ReadToEndAsync();

            try
            {
                var imported = ConfigExporter.Import(json);
                await configStore.SaveAsync(imported);
                return Results.Ok(new { message = "Config imported. Re-enter API tokens for each connection." });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapGet("/api/config/preview", async (IConfigStore configStore) =>
        {
            var path = configStore.ConfigFilePath;
            if (!File.Exists(path))
                return Results.Ok(new { content = "{}" });

            var content = await File.ReadAllTextAsync(path);
            return Results.Ok(new { content });
        });
    }
}

public sealed class SettingsDto
{
    public int RefreshMinutes { get; set; }
    public QueueDefaults Defaults { get; set; } = new();
    public string? ConfigFilePath { get; init; }
}
