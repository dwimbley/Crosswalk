namespace Crosswalk.Core.Models;

public sealed class JiraTicket
{
    public required string Key { get; init; }
    public required string Summary { get; init; }
    public required string Status { get; init; }
    public required string StatusCategoryKey { get; init; }
    public required NormalizedCategory NormalizedCategory { get; set; }
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
}

public sealed class BoardRef
{
    public required int Id { get; init; }
    public required string Name { get; init; }
}
