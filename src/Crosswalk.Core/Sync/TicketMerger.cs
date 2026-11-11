using Crosswalk.Core.Models;

namespace Crosswalk.Core.Sync;

/// <summary>
/// Merges tickets from multiple connections:
/// - Same key + same site → one row (merge board lists)
/// - Same key + different sites → separate rows (site badge distinguishes)
/// </summary>
public static class TicketMerger
{
    public static List<JiraTicket> DeduplicateWithinSite(IEnumerable<JiraTicket> tickets)
    {
        var result = new Dictionary<string, JiraTicket>();

        foreach (var ticket in tickets)
        {
            if (result.TryGetValue(ticket.Key, out var existing))
            {
                // Merge boards from the duplicate
                foreach (var board in ticket.Boards)
                {
                    if (!existing.Boards.Any(b => b.Id == board.Id))
                        existing.Boards.Add(board);
                }
            }
            else
            {
                result[ticket.Key] = ticket;
            }
        }

        return [.. result.Values];
    }
}
