using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Crosswalk.Core.Models;
using Crosswalk.Core.Sync;
using Microsoft.Extensions.Logging;

namespace Crosswalk.Core.Jira;

public sealed class JiraClient : IJiraClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<JiraClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly string[] RequestedFields =
    [
        "summary", "status", "priority", "duedate", "updated", "created",
        "reporter", "assignee", "project", "description"
    ];

    public JiraClient(IHttpClientFactory httpClientFactory, ILogger<JiraClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<JiraUser> GetMyselfAsync(string baseUrl, string email, string apiToken, CancellationToken ct = default)
    {
        using var client = CreateClient(email, apiToken);
        var response = await client.GetAsync($"{baseUrl}/rest/api/3/myself", ct);
        await EnsureSuccessAsync(response, "myself", ct);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        return new JiraUser
        {
            AccountId = json.GetProperty("accountId").GetString()!,
            DisplayName = json.GetProperty("displayName").GetString()!,
            EmailAddress = json.TryGetProperty("emailAddress", out var e) ? e.GetString() : null
        };
    }

    public async Task<List<JiraProject>> GetProjectsAsync(string baseUrl, string email, string apiToken, CancellationToken ct = default)
    {
        var projects = new List<JiraProject>();
        var startAt = 0;
        const int maxResults = 50;

        while (true)
        {
            using var client = CreateClient(email, apiToken);
            var url = $"{baseUrl}/rest/api/3/project/search?startAt={startAt}&maxResults={maxResults}";
            var response = await client.GetAsync(url, ct);
            await EnsureSuccessAsync(response, "project/search", ct);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            var values = json.GetProperty("values");

            foreach (var proj in values.EnumerateArray())
            {
                projects.Add(new JiraProject
                {
                    Key = proj.GetProperty("key").GetString()!,
                    Name = proj.GetProperty("name").GetString()!
                });
            }

            var total = json.GetProperty("total").GetInt32();
            startAt += values.GetArrayLength();
            if (startAt >= total) break;
        }

        return projects;
    }

    public async Task<List<JiraBoard>> GetBoardsAsync(string baseUrl, string email, string apiToken, string? projectKeyOrId = null, CancellationToken ct = default)
    {
        var boards = new List<JiraBoard>();
        var startAt = 0;
        const int maxResults = 50;

        while (true)
        {
            using var client = CreateClient(email, apiToken);
            var url = $"{baseUrl}/rest/agile/1.0/board?startAt={startAt}&maxResults={maxResults}";
            if (!string.IsNullOrEmpty(projectKeyOrId))
                url += $"&projectKeyOrId={Uri.EscapeDataString(projectKeyOrId)}";

            var response = await client.GetAsync(url, ct);
            await EnsureSuccessAsync(response, "agile/board", ct);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            var values = json.GetProperty("values");

            foreach (var board in values.EnumerateArray())
            {
                string? projectKey = null;
                if (board.TryGetProperty("location", out var loc) &&
                    loc.TryGetProperty("projectKey", out var pk))
                {
                    projectKey = pk.GetString();
                }

                boards.Add(new JiraBoard
                {
                    Id = board.GetProperty("id").GetInt32(),
                    Name = board.GetProperty("name").GetString()!,
                    ProjectKey = projectKey
                });
            }

            var isLast = json.TryGetProperty("isLast", out var il) && il.GetBoolean();
            if (isLast || values.GetArrayLength() == 0) break;
            startAt += values.GetArrayLength();
        }

        return boards;
    }

    public async Task<List<JiraTicket>> SearchTicketsAsync(string baseUrl, string email, string apiToken, string jql, CancellationToken ct = default)
    {
        var tickets = new List<JiraTicket>();
        string? nextPageToken = null;
        const int maxResults = 100;

        while (true)
        {
            using var client = CreateClient(email, apiToken);

            // /rest/api/3/search/jql uses GET with query parameters
            var fieldsParam = Uri.EscapeDataString(string.Join(",", RequestedFields));
            var jqlParam = Uri.EscapeDataString(jql);
            var url = $"{baseUrl}/rest/api/3/search/jql?jql={jqlParam}&fields={fieldsParam}&maxResults={maxResults}";
            if (nextPageToken != null)
                url += $"&nextPageToken={Uri.EscapeDataString(nextPageToken)}";

            var response = await client.GetAsync(url, ct);
            await EnsureSuccessAsync(response, "search/jql", ct);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            var issues = json.GetProperty("issues");

            foreach (var issue in issues.EnumerateArray())
            {
                tickets.Add(ParseIssue(issue));
            }

            // New endpoint uses cursor-based pagination via nextPageToken
            if (json.TryGetProperty("nextPageToken", out var tokenEl) &&
                tokenEl.ValueKind == JsonValueKind.String)
            {
                nextPageToken = tokenEl.GetString();
                if (string.IsNullOrEmpty(nextPageToken)) break;
            }
            else
            {
                break;
            }
        }

        return tickets;
    }

    public async Task<List<string>> GetBoardIssueKeysAsync(string baseUrl, string email, string apiToken, int boardId, IEnumerable<string> issueKeys, CancellationToken ct = default)
    {
        // Query the board's issues filtered to our known keys
        var keys = issueKeys.ToList();
        if (keys.Count == 0) return [];

        var matchedKeys = new List<string>();
        var startAt = 0;
        const int maxResults = 100;

        // Use JQL filter to narrow: key in (...)
        var keyList = string.Join(",", keys.Select(k => $"\"{k}\""));
        var jql = $"key in ({keyList})";

        while (true)
        {
            using var client = CreateClient(email, apiToken);
            var url = $"{baseUrl}/rest/agile/1.0/board/{boardId}/issue?startAt={startAt}&maxResults={maxResults}&jql={Uri.EscapeDataString(jql)}&fields=key";

            var response = await client.GetAsync(url, ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // Board may not exist or be inaccessible
                _logger.LogWarning("Board {BoardId} not found or inaccessible", boardId);
                return [];
            }

            await EnsureSuccessAsync(response, $"board/{boardId}/issue", ct);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            var issues = json.GetProperty("issues");

            foreach (var issue in issues.EnumerateArray())
            {
                matchedKeys.Add(issue.GetProperty("key").GetString()!);
            }

            var total = json.GetProperty("total").GetInt32();
            startAt += issues.GetArrayLength();
            if (startAt >= total) break;
        }

        return matchedKeys;
    }

    private HttpClient CreateClient(string email, string apiToken)
    {
        var client = _httpClientFactory.CreateClient("Jira");
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{email}:{apiToken}"));
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Crosswalk/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        client.Timeout = TimeSpan.FromSeconds(30);
        return client;
    }

    private static JiraTicket ParseIssue(JsonElement issue)
    {
        var fields = issue.GetProperty("fields");

        var statusObj = fields.GetProperty("status");
        var statusName = statusObj.GetProperty("name").GetString()!;
        var statusCatKey = statusObj.GetProperty("statusCategory").GetProperty("key").GetString()!;

        var priorityObj = fields.GetProperty("priority");
        var priorityName = priorityObj.GetProperty("name").GetString()!;
        var priorityId = priorityObj.TryGetProperty("id", out var pid) ? pid.GetString() : "3";

        var projectObj = fields.GetProperty("project");

        string? descriptionHtml = null;
        if (fields.TryGetProperty("description", out var desc) && desc.ValueKind != JsonValueKind.Null)
        {
            descriptionHtml = AdfConverter.ToHtml(desc);
        }

        return new JiraTicket
        {
            Key = issue.GetProperty("key").GetString()!,
            Summary = fields.GetProperty("summary").GetString()!,
            Status = statusName,
            StatusCategoryKey = statusCatKey,
            NormalizedCategory = StatusNormalizer.Normalize(statusName, statusCatKey),
            Priority = priorityName,
            PriorityOrder = MapPriorityOrder(priorityName, priorityId),
            DueDate = fields.TryGetProperty("duedate", out var dd) && dd.ValueKind != JsonValueKind.Null
                ? DateTimeOffset.Parse(dd.GetString()!)
                : null,
            Updated = DateTimeOffset.Parse(fields.GetProperty("updated").GetString()!),
            Created = DateTimeOffset.Parse(fields.GetProperty("created").GetString()!),
            Reporter = GetUserDisplayName(fields, "reporter"),
            Assignee = GetUserDisplayName(fields, "assignee"),
            AssigneeAccountId = GetUserAccountId(fields, "assignee"),
            ProjectKey = projectObj.GetProperty("key").GetString()!,
            ProjectName = projectObj.GetProperty("name").GetString()!,
            DescriptionHtml = descriptionHtml
        };
    }

    private static string GetUserDisplayName(JsonElement fields, string fieldName)
    {
        if (fields.TryGetProperty(fieldName, out var user) && user.ValueKind != JsonValueKind.Null)
            return user.GetProperty("displayName").GetString()!;
        return "Unassigned";
    }

    private static string GetUserAccountId(JsonElement fields, string fieldName)
    {
        if (fields.TryGetProperty(fieldName, out var user) && user.ValueKind != JsonValueKind.Null)
            return user.GetProperty("accountId").GetString()!;
        return "";
    }

    private static int MapPriorityOrder(string name, string? id)
    {
        // Jira default priority IDs: 1=Highest, 2=High, 3=Medium, 4=Low, 5=Lowest
        if (int.TryParse(id, out var numericId) && numericId >= 1 && numericId <= 5)
            return numericId;

        return name.ToLowerInvariant() switch
        {
            "highest" => 1,
            "high" => 2,
            "medium" => 3,
            "low" => 4,
            "lowest" => 5,
            _ => 3
        };
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string endpoint, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync(ct);

        throw response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                new JiraAuthException($"Authentication failed for {endpoint}: {response.StatusCode}"),
            HttpStatusCode.TooManyRequests =>
                new JiraRateLimitException(
                    response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(30),
                    $"Rate limited on {endpoint}"),
            _ => new JiraApiException($"Jira API error on {endpoint}: {(int)response.StatusCode} {body}")
        };
    }
}
