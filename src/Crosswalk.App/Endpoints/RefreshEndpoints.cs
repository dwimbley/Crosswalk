using Crosswalk.App.Services;

namespace Crosswalk.App.Endpoints;

public static class RefreshEndpoints
{
    public static void MapRefreshEndpoints(this WebApplication app)
    {
        app.MapPost("/api/refresh", async (AppState state) =>
        {
            _ = Task.Run(() => state.RefreshAllAsync());
            return Results.Accepted();
        });

        app.MapPost("/api/connections/{id}/refresh", async (string id, AppState state) =>
        {
            _ = Task.Run(() => state.RefreshConnectionAsync(id));
            return Results.Accepted();
        });
    }
}
