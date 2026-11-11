using Crosswalk.Core.Configuration;
using Crosswalk.Core.Sync;

namespace Crosswalk.Tests;

public class SyncServiceTests
{
    [Fact]
    public void BuildJql_BasicConnection()
    {
        var conn = new ConnectionConfig
        {
            Id = "test",
            DisplayName = "Test",
            BaseUrl = "https://test.atlassian.net",
            Email = "me@test.com",
            Projects = ["CLM", "PLAT"]
        };

        var jql = SyncService.BuildJql(conn);

        Assert.Contains("assignee = currentUser()", jql);
        Assert.Contains("statusCategory != Done", jql);
        Assert.Contains("project in (\"CLM\", \"PLAT\")", jql);
        Assert.Contains("ORDER BY priority ASC, updated DESC", jql);
    }

    [Fact]
    public void BuildJql_NoProjects()
    {
        var conn = new ConnectionConfig
        {
            Id = "test",
            DisplayName = "Test",
            BaseUrl = "https://test.atlassian.net",
            Email = "me@test.com"
        };

        var jql = SyncService.BuildJql(conn);

        Assert.Contains("assignee = currentUser()", jql);
        Assert.Contains("statusCategory != Done", jql);
    }

    [Fact]
    public void BuildJql_WithExtraJql()
    {
        var conn = new ConnectionConfig
        {
            Id = "test",
            DisplayName = "Test",
            BaseUrl = "https://test.atlassian.net",
            Email = "me@test.com",
            Projects = ["CLM"],
            ExtraJql = "labels = urgent"
        };

        var jql = SyncService.BuildJql(conn);

        Assert.Contains("(labels = urgent)", jql);
        Assert.Contains("assignee = currentUser()", jql);
    }
}
