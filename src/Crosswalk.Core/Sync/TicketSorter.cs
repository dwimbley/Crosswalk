using Crosswalk.Core.Models;

namespace Crosswalk.Core.Sync;

public static class TicketSorter
{
    public static List<TicketViewModel> Sort(
        IEnumerable<TicketViewModel> tickets,
        string sortField,
        bool descending = false)
    {
        IOrderedEnumerable<TicketViewModel> sorted = sortField.ToLowerInvariant() switch
        {
            "key" => descending
                ? tickets.OrderByDescending(t => t.ProjectKey).ThenByDescending(t => ExtractNumber(t.Key))
                : tickets.OrderBy(t => t.ProjectKey).ThenBy(t => ExtractNumber(t.Key)),

            "site" => descending
                ? tickets.OrderByDescending(t => t.SiteDisplayName)
                : tickets.OrderBy(t => t.SiteDisplayName),

            "project" => descending
                ? tickets.OrderByDescending(t => t.ProjectName)
                : tickets.OrderBy(t => t.ProjectName),

            "status" => descending
                ? tickets.OrderByDescending(t => (int)t.NormalizedCategory)
                : tickets.OrderBy(t => (int)t.NormalizedCategory),

            "priority" => descending
                ? tickets.OrderByDescending(t => t.PriorityOrder)
                : tickets.OrderBy(t => t.PriorityOrder),

            "due" => descending
                ? tickets.OrderBy(t => t.DueDate.HasValue ? 0 : 1)
                      .ThenByDescending(t => t.DueDate)
                : tickets.OrderBy(t => t.DueDate.HasValue ? 0 : 1)
                      .ThenBy(t => t.DueDate),

            "updated" or _ => descending
                ? tickets.OrderByDescending(t => t.Updated)
                : tickets.OrderBy(t => t.Updated),
        };

        return [.. sorted];
    }

    /// <summary>
    /// Default sort: priority highest first (lowest PriorityOrder = highest priority).
    /// </summary>
    public static List<TicketViewModel> DefaultSort(IEnumerable<TicketViewModel> tickets)
        => Sort(tickets, "priority", descending: false);

    private static int ExtractNumber(string key)
    {
        var dashIndex = key.LastIndexOf('-');
        if (dashIndex >= 0 && int.TryParse(key.AsSpan(dashIndex + 1), out var num))
            return num;
        return 0;
    }
}
