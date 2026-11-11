using System.Text.RegularExpressions;
using Crosswalk.Core.Models;

namespace Crosswalk.Core.Sync;

public static partial class StatusNormalizer
{
    /// <summary>
    /// Maps a Jira status to a normalized category using:
    /// 1. Per-connection overrides (highest priority)
    /// 2. Name heuristics
    /// 3. Jira statusCategory fallback
    /// </summary>
    public static NormalizedCategory Normalize(
        string statusName,
        string statusCategoryKey,
        IReadOnlyDictionary<string, string>? connectionStatusMap = null)
    {
        // 1. Per-connection override (highest priority)
        if (connectionStatusMap is not null
            && connectionStatusMap.TryGetValue(statusName, out var overrideValue))
        {
            if (TryParseCategory(overrideValue, out var overrideCat))
                return overrideCat;
        }

        // 2. Done category always wins — never let heuristics reclassify a Done ticket
        if (string.Equals(statusCategoryKey, "done", StringComparison.OrdinalIgnoreCase))
            return NormalizedCategory.Done;

        // 3. Name heuristics (only for non-Done tickets)
        if (InReviewPattern().IsMatch(statusName))
            return NormalizedCategory.InReview;

        if (BlockedPattern().IsMatch(statusName))
            return NormalizedCategory.Blocked;

        // 4. Jira statusCategory fallback
        return statusCategoryKey.ToLowerInvariant() switch
        {
            "new" => NormalizedCategory.ToDo,
            "indeterminate" => NormalizedCategory.InProgress,
            _ => NormalizedCategory.ToDo
        };
    }

    private static bool TryParseCategory(string value, out NormalizedCategory category)
    {
        // Accept both enum names and lowercase slugs
        var normalized = value.Trim().ToLowerInvariant().Replace(" ", "");
        (bool found, NormalizedCategory cat) = normalized switch
        {
            "todo" or "to_do" or "to-do" => (true, NormalizedCategory.ToDo),
            "inprogress" or "in_progress" or "in-progress" => (true, NormalizedCategory.InProgress),
            "inreview" or "in_review" or "in-review" => (true, NormalizedCategory.InReview),
            "blocked" => (true, NormalizedCategory.Blocked),
            "done" => (true, NormalizedCategory.Done),
            _ => (false, default)
        };
        category = cat;
        return found;
    }

    [GeneratedRegex(@"\b(review|qa|verify|verification|code\s*review)\b", RegexOptions.IgnoreCase)]
    private static partial Regex InReviewPattern();

    [GeneratedRegex(@"\b(blocked|on\s*hold|waiting|impediment)\b", RegexOptions.IgnoreCase)]
    private static partial Regex BlockedPattern();
}
