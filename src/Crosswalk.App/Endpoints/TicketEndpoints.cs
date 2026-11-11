using Crosswalk.App.Services;

namespace Crosswalk.App.Endpoints;

public static class TicketEndpoints
{
    public static void MapTicketEndpoints(this WebApplication app)
    {
        app.MapGet("/api/tickets", (AppState state) =>
        {
            var tickets = state.GetAllTickets();
            return Results.Ok(tickets);
        });
    }
}
