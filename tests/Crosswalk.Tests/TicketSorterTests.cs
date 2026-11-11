using Crosswalk.Core.Models;
using Crosswalk.Core.Sync;

namespace Crosswalk.Tests;

public class TicketSorterTests
{
    [Fact]
    public void Sort_ByPriority_HighestFirst()
    {
        var tickets = new[]
        {
            MakeVm("T-1", priorityOrder: 4, priority: "Low"),
            MakeVm("T-2", priorityOrder: 1, priority: "Highest"),
            MakeVm("T-3", priorityOrder: 3, priority: "Medium"),
        };

        var result = TicketSorter.Sort(tickets, "priority");

        Assert.Equal("T-2", result[0].Key);
        Assert.Equal("T-3", result[1].Key);
        Assert.Equal("T-1", result[2].Key);
    }

    [Fact]
    public void Sort_ByDue_NullsLast()
    {
        var now = DateTimeOffset.UtcNow;
        var tickets = new[]
        {
            MakeVm("T-1", due: null),
            MakeVm("T-2", due: now.AddDays(1)),
            MakeVm("T-3", due: now.AddDays(-1)),
        };

        var result = TicketSorter.Sort(tickets, "due");

        Assert.Equal("T-3", result[0].Key); // earliest
        Assert.Equal("T-2", result[1].Key); // next
        Assert.Equal("T-1", result[2].Key); // null last
    }

    [Fact]
    public void Sort_ByDue_Descending_NullsLast()
    {
        var now = DateTimeOffset.UtcNow;
        var tickets = new[]
        {
            MakeVm("T-1", due: null),
            MakeVm("T-2", due: now.AddDays(1)),
            MakeVm("T-3", due: now.AddDays(-1)),
        };

        var result = TicketSorter.Sort(tickets, "due", descending: true);

        // Descending: nulls still last, then latest first
        Assert.Equal("T-2", result[0].Key);
        Assert.Equal("T-3", result[1].Key);
        Assert.Equal("T-1", result[2].Key);
    }

    [Fact]
    public void Sort_ByUpdated_Default()
    {
        var now = DateTimeOffset.UtcNow;
        var tickets = new[]
        {
            MakeVm("T-1", updated: now.AddHours(-2)),
            MakeVm("T-2", updated: now),
            MakeVm("T-3", updated: now.AddHours(-1)),
        };

        var result = TicketSorter.Sort(tickets, "updated");

        Assert.Equal("T-1", result[0].Key);
        Assert.Equal("T-3", result[1].Key);
        Assert.Equal("T-2", result[2].Key);
    }

    [Fact]
    public void Sort_ByKey_NumericPart()
    {
        var tickets = new[]
        {
            MakeVm("CLM-100"),
            MakeVm("CLM-9"),
            MakeVm("CLM-42"),
        };

        var result = TicketSorter.Sort(tickets, "key");

        Assert.Equal("CLM-9", result[0].Key);
        Assert.Equal("CLM-42", result[1].Key);
        Assert.Equal("CLM-100", result[2].Key);
    }

    [Fact]
    public void DefaultSort_UsesPriority()
    {
        var tickets = new[]
        {
            MakeVm("T-1", priorityOrder: 3, priority: "Medium"),
            MakeVm("T-2", priorityOrder: 1, priority: "Highest"),
        };

        var result = TicketSorter.DefaultSort(tickets);
        Assert.Equal("T-2", result[0].Key);
    }

    private static TicketViewModel MakeVm(
        string key,
        int priorityOrder = 3,
        string priority = "Medium",
        DateTimeOffset? due = null,
        DateTimeOffset? updated = null) => new()
    {
        Key = key,
        Summary = $"Test {key}",
        Status = "To Do",
        StatusCategoryKey = "new",
        NormalizedCategory = NormalizedCategory.ToDo,
        Priority = priority,
        PriorityOrder = priorityOrder,
        DueDate = due,
        Updated = updated ?? DateTimeOffset.UtcNow,
        Created = DateTimeOffset.UtcNow,
        Reporter = "reporter",
        Assignee = "assignee",
        AssigneeAccountId = "acc-123",
        ProjectKey = key.Split('-')[0],
        ProjectName = "Test Project",
        SiteId = "test-site",
        SiteDisplayName = "Test Site",
        SiteBaseUrl = "https://test.atlassian.net",
        SiteEmail = "test@test.com",
        JiraUrl = $"https://test.atlassian.net/browse/{key}",
        IsStale = false
    };
}
