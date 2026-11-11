using System.Net;
using Crosswalk.Core.Jira;
using Crosswalk.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Crosswalk.Tests;

public class JiraClientTests : IDisposable
{
    private readonly MockHttpHandler _handler;
    private readonly JiraClient _client;
    private const string BaseUrl = "https://harborline.atlassian.net";
    private const string Email = "me@harborline.com";
    private const string Token = "test-token";

    public JiraClientTests()
    {
        _handler = new MockHttpHandler();
        var factory = new MockHttpClientFactory(_handler);
        _client = new JiraClient(factory, NullLogger<JiraClient>.Instance);
    }

    public void Dispose() => _handler.Dispose();

    [Fact]
    public async Task GetMyself_ReturnsUser()
    {
        _handler.AddResponse("/rest/api/3/myself", HttpStatusCode.OK,
            LoadFixture("myself.json"));

        var user = await _client.GetMyselfAsync(BaseUrl, Email, Token);

        Assert.Equal("David Wimbley", user.DisplayName);
        Assert.Equal("5f8b3c7e1a2b3c4d5e6f7890", user.AccountId);
    }

    [Fact]
    public async Task GetProjects_ReturnsProjects()
    {
        _handler.AddResponse("/rest/api/3/project/search", HttpStatusCode.OK,
            LoadFixture("projects.json"));

        var projects = await _client.GetProjectsAsync(BaseUrl, Email, Token);

        Assert.Equal(2, projects.Count);
        Assert.Equal("CLM", projects[0].Key);
        Assert.Equal("PLAT", projects[1].Key);
    }

    [Fact]
    public async Task GetBoards_ReturnsBoards()
    {
        _handler.AddResponse("/rest/agile/1.0/board", HttpStatusCode.OK,
            LoadFixture("boards.json"));

        var boards = await _client.GetBoardsAsync(BaseUrl, Email, Token);

        Assert.Equal(2, boards.Count);
        Assert.Equal("Claims Sprint Board", boards[0].Name);
        Assert.Equal("CLM", boards[0].ProjectKey);
    }

    [Fact]
    public async Task SearchTickets_Paginates()
    {
        var handler = new MockHttpHandler();
        handler.AddSequentialResponses("/rest/api/3/search",
            (HttpStatusCode.OK, LoadFixture("search_page1.json")),
            (HttpStatusCode.OK, LoadFixture("search_page2.json")));

        var factory = new MockHttpClientFactory(handler);
        var client = new JiraClient(factory, NullLogger<JiraClient>.Instance);

        var tickets = await client.SearchTicketsAsync(BaseUrl, Email, Token,
            "assignee = currentUser()");

        Assert.Equal(3, tickets.Count);
        Assert.Equal("CLM-100", tickets[0].Key);
        Assert.Equal("CLM-101", tickets[1].Key);
        Assert.Equal("CLM-102", tickets[2].Key);
    }

    [Fact]
    public async Task SearchTickets_ParsesFields()
    {
        _handler.AddResponse("/rest/api/3/search", HttpStatusCode.OK,
            LoadFixture("search_single.json"));

        var tickets = await _client.SearchTicketsAsync(BaseUrl, Email, Token, "assignee = currentUser()");
        var ticket = tickets[0];

        Assert.Equal("CLM-100", ticket.Key);
        Assert.Equal("Test ticket one", ticket.Summary);
        Assert.Equal("In Progress", ticket.Status);
        Assert.Equal("indeterminate", ticket.StatusCategoryKey);
        Assert.Equal(NormalizedCategory.InProgress, ticket.NormalizedCategory);
        Assert.Equal("High", ticket.Priority);
        Assert.Equal(2, ticket.PriorityOrder);
        Assert.NotNull(ticket.DueDate);
        Assert.Equal("David Wimbley", ticket.Assignee);
        Assert.Equal("CLM", ticket.ProjectKey);
    }

    [Fact]
    public async Task SearchTickets_StatusHeuristic_CodeReview_MapsInReview()
    {
        _handler.AddResponse("/rest/api/3/search", HttpStatusCode.OK,
            LoadFixture("search_single.json"));

        var tickets = await _client.SearchTicketsAsync(BaseUrl, Email, Token, "assignee = currentUser()");
        var codeReviewTicket = tickets.First(t => t.Status == "Code Review");

        Assert.Equal(NormalizedCategory.InReview, codeReviewTicket.NormalizedCategory);
    }

    [Fact]
    public async Task SearchTickets_StatusHeuristic_Blocked_MapsBlocked()
    {
        _handler.AddSequentialResponses("/rest/api/3/search",
            (HttpStatusCode.OK, LoadFixture("search_page1.json")),
            (HttpStatusCode.OK, LoadFixture("search_page2.json")));

        var factory = new MockHttpClientFactory(_handler);
        var client = new JiraClient(factory, NullLogger<JiraClient>.Instance);

        var tickets = await client.SearchTicketsAsync(BaseUrl, Email, Token, "assignee = currentUser()");
        var blockedTicket = tickets.First(t => t.Status == "Blocked");

        Assert.Equal(NormalizedCategory.Blocked, blockedTicket.NormalizedCategory);
    }

    [Fact]
    public async Task SearchTickets_ParsesAdfDescription()
    {
        _handler.AddResponse("/rest/api/3/search", HttpStatusCode.OK,
            LoadFixture("search_single.json"));

        var tickets = await _client.SearchTicketsAsync(BaseUrl, Email, Token, "assignee = currentUser()");

        Assert.Equal("<p>This is a test description.</p>", tickets[0].DescriptionHtml);
        Assert.Null(tickets[1].DescriptionHtml); // null description
    }

    [Fact]
    public async Task GetMyself_401_ThrowsAuthException()
    {
        _handler.AddResponse("/rest/api/3/myself", HttpStatusCode.Unauthorized,
            """{"message":"Unauthorized"}""");

        await Assert.ThrowsAsync<JiraAuthException>(
            () => _client.GetMyselfAsync(BaseUrl, Email, Token));
    }

    [Fact]
    public async Task SearchTickets_429_ThrowsRateLimitException()
    {
        _handler.AddResponse(
            req => req.RequestUri?.ToString().Contains("/rest/api/3/search") == true,
            HttpStatusCode.TooManyRequests,
            """{"message":"Rate limit exceeded"}""");

        var ex = await Assert.ThrowsAsync<JiraRateLimitException>(
            () => _client.SearchTicketsAsync(BaseUrl, Email, Token, "assignee = currentUser()"));

        Assert.True(ex.RetryAfter > TimeSpan.Zero);
    }

    [Fact]
    public async Task SearchTickets_503_ThrowsApiException()
    {
        _handler.AddResponse(
            req => req.RequestUri?.ToString().Contains("/rest/api/3/search") == true,
            HttpStatusCode.ServiceUnavailable,
            """{"message":"Service unavailable"}""");

        await Assert.ThrowsAsync<JiraApiException>(
            () => _client.SearchTicketsAsync(BaseUrl, Email, Token, "assignee = currentUser()"));
    }

    [Fact]
    public async Task GetMyself_SetsBasicAuth()
    {
        _handler.AddResponse("/rest/api/3/myself", HttpStatusCode.OK,
            LoadFixture("myself.json"));

        await _client.GetMyselfAsync(BaseUrl, Email, Token);

        var request = _handler.SentRequests[0];
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Basic", request.Headers.Authorization.Scheme);
    }

    [Fact]
    public async Task GetMyself_SetsUserAgent()
    {
        _handler.AddResponse("/rest/api/3/myself", HttpStatusCode.OK,
            LoadFixture("myself.json"));

        await _client.GetMyselfAsync(BaseUrl, Email, Token);

        var request = _handler.SentRequests[0];
        Assert.Contains(request.Headers.UserAgent, ua => ua.Product?.Name == "Crosswalk");
    }

    private static string LoadFixture(string name)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
        return File.ReadAllText(path);
    }
}

/// <summary>
/// Simple IHttpClientFactory that returns clients using a shared handler.
/// </summary>
file sealed class MockHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}
