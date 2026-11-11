using Crosswalk.App.Services;
using Crosswalk.Core.Configuration;
using Crosswalk.Core.Jira;
using Crosswalk.Core.Secrets;

namespace Crosswalk.App.Endpoints;

public static class ConnectionEndpoints
{
    public static void MapConnectionEndpoints(this WebApplication app)
    {
        app.MapGet("/api/connections", (AppState state) =>
        {
            return Results.Ok(state.GetConnectionStatuses());
        });

        app.MapPost("/api/connections/test", async (TestConnectionRequest req, IJiraClient jira) =>
        {
            try
            {
                var user = await jira.GetMyselfAsync(req.BaseUrl, req.Email, req.ApiToken);
                var projects = await jira.GetProjectsAsync(req.BaseUrl, req.Email, req.ApiToken);
                var boards = await jira.GetBoardsAsync(req.BaseUrl, req.Email, req.ApiToken);

                return Results.Ok(new TestConnectionResult
                {
                    Success = true,
                    DisplayName = user.DisplayName,
                    AccountId = user.AccountId,
                    ProjectCount = projects.Count,
                    BoardCount = boards.Count,
                    Projects = projects,
                    Boards = boards
                });
            }
            catch (JiraAuthException)
            {
                return Results.Ok(new TestConnectionResult
                {
                    Success = false,
                    Error = "Authentication failed. Check your email and API token."
                });
            }
            catch (Exception ex)
            {
                return Results.Ok(new TestConnectionResult
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        });

        app.MapPost("/api/connections", async (
            ConnectionConfig conn,
            IConfigStore configStore,
            ISecretStore secretStore,
            AppState state,
            HttpContext ctx) =>
        {
            // Extract token from a custom header (not stored in config body)
            var token = ctx.Request.Headers["X-Api-Token"].FirstOrDefault();

            var config = await configStore.LoadAsync();

            if (config.Connections.Any(c => c.Id == conn.Id))
                return Results.Conflict($"Connection '{conn.Id}' already exists.");

            conn.Auth = new AuthConfig { Type = "apiToken", SecretRef = $"Crosswalk/{conn.Id}" };
            config.Connections.Add(conn);
            await configStore.SaveAsync(config);

            if (!string.IsNullOrEmpty(token))
                await secretStore.SetAsync($"Crosswalk/{conn.Id}", token);

            _ = Task.Run(() => state.RefreshConnectionAsync(conn.Id));

            return Results.Created($"/api/connections/{conn.Id}", conn);
        });

        app.MapPut("/api/connections/{id}", async (
            string id,
            ConnectionConfig conn,
            IConfigStore configStore,
            ISecretStore secretStore,
            AppState state,
            HttpContext ctx) =>
        {
            var token = ctx.Request.Headers["X-Api-Token"].FirstOrDefault();

            var config = await configStore.LoadAsync();
            var index = config.Connections.FindIndex(c => c.Id == id);
            if (index < 0)
                return Results.NotFound();

            conn.Id = id;
            conn.Auth = new AuthConfig { Type = "apiToken", SecretRef = $"Crosswalk/{id}" };
            config.Connections[index] = conn;
            await configStore.SaveAsync(config);

            if (!string.IsNullOrEmpty(token))
                await secretStore.SetAsync($"Crosswalk/{id}", token);

            _ = Task.Run(() => state.RefreshConnectionAsync(id));

            return Results.Ok(conn);
        });

        app.MapDelete("/api/connections/{id}", async (
            string id,
            IConfigStore configStore,
            ISecretStore secretStore,
            AppState state) =>
        {
            var config = await configStore.LoadAsync();
            var removed = config.Connections.RemoveAll(c => c.Id == id);
            if (removed == 0)
                return Results.NotFound();

            await configStore.SaveAsync(config);
            await secretStore.DeleteAsync($"Crosswalk/{id}");

            return Results.NoContent();
        });
    }
}

public sealed class TestConnectionRequest
{
    public required string BaseUrl { get; init; }
    public required string Email { get; init; }
    public required string ApiToken { get; init; }
}

public sealed class TestConnectionResult
{
    public bool Success { get; init; }
    public string? DisplayName { get; init; }
    public string? AccountId { get; init; }
    public int ProjectCount { get; init; }
    public int BoardCount { get; init; }
    public string? Error { get; init; }
    public List<JiraProject>? Projects { get; init; }
    public List<JiraBoard>? Boards { get; init; }
}
