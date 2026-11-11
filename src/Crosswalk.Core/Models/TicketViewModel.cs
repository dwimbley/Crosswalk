namespace Crosswalk.Core.Models;

public sealed class TicketViewModel
{
    public required string Key { get; init; }
    public required string Summary { get; init; }
    public required string Status { get; init; }
    public required string StatusCategoryKey { get; init; }
    public required NormalizedCategory NormalizedCategory { get; init; }
    public required string Priority { get; init; }
    public int PriorityOrder { get; init; }
    public DateTimeOffset? DueDate { get; init; }
    public required DateTimeOffset Updated { get; init; }
    public required DateTimeOffset Created { get; init; }
    public required string Reporter { get; init; }
    public required string Assignee { get; init; }
    public required string AssigneeAccountId { get; init; }
    public required string ProjectKey { get; init; }
    public required string ProjectName { get; init; }
    public string? DescriptionHtml { get; init; }
    public List<BoardRef> Boards { get; init; } = [];

    // Site context
    public required string SiteId { get; init; }
    public required string SiteDisplayName { get; init; }
    public string? SiteColor { get; init; }
    public required string SiteBaseUrl { get; init; }
    public required string SiteEmail { get; init; }

    // Computed
    public required string JiraUrl { get; init; }
    public required bool IsStale { get; init; }
    public DateTimeOffset? LastSuccessfulRefresh { get; init; }

    /// <summary>
    /// Unique identity: combines site ID and issue key to distinguish the same key across sites.
    /// </summary>
    public string UniqueId => $"{SiteId}:{Key}";
}
