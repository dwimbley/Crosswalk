using Crosswalk.Core.Models;

namespace Crosswalk.Core.Jira;

public interface IJiraClient
{
    Task<JiraUser> GetMyselfAsync(string baseUrl, string email, string apiToken, CancellationToken ct = default);
    Task<List<JiraProject>> GetProjectsAsync(string baseUrl, string email, string apiToken, CancellationToken ct = default);
    Task<List<JiraBoard>> GetBoardsAsync(string baseUrl, string email, string apiToken, string? projectKeyOrId = null, CancellationToken ct = default);
    Task<List<JiraTicket>> SearchTicketsAsync(string baseUrl, string email, string apiToken, string jql, CancellationToken ct = default);
    Task<List<string>> GetBoardIssueKeysAsync(string baseUrl, string email, string apiToken, int boardId, IEnumerable<string> issueKeys, CancellationToken ct = default);
}

public sealed class JiraUser
{
    public required string AccountId { get; init; }
    public required string DisplayName { get; init; }
    public string? EmailAddress { get; init; }
}

public sealed class JiraProject
{
    public required string Key { get; init; }
    public required string Name { get; init; }
}

public sealed class JiraBoard
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public string? ProjectKey { get; init; }
}
