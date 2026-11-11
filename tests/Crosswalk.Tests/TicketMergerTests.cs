using Crosswalk.Core.Models;
using Crosswalk.Core.Sync;

namespace Crosswalk.Tests;

public class TicketMergerTests
{
    [Fact]
    public void SameKeyWithinSite_MergesBoards()
    {
        var tickets = new[]
        {
            MakeTicket("CLM-100", [new BoardRef { Id = 1, Name = "Board A" }]),
            MakeTicket("CLM-100", [new BoardRef { Id = 2, Name = "Board B" }]),
        };

        var result = TicketMerger.DeduplicateWithinSite(tickets);

        Assert.Single(result);
        Assert.Equal(2, result[0].Boards.Count);
        Assert.Contains(result[0].Boards, b => b.Name == "Board A");
        Assert.Contains(result[0].Boards, b => b.Name == "Board B");
    }

    [Fact]
    public void DifferentKeys_NotMerged()
    {
        var tickets = new[]
        {
            MakeTicket("CLM-100", []),
            MakeTicket("CLM-200", []),
        };

        var result = TicketMerger.DeduplicateWithinSite(tickets);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void DuplicateBoard_NotAddedTwice()
    {
        var tickets = new[]
        {
            MakeTicket("CLM-100", [new BoardRef { Id = 1, Name = "Board A" }]),
            MakeTicket("CLM-100", [new BoardRef { Id = 1, Name = "Board A" }]),
        };

        var result = TicketMerger.DeduplicateWithinSite(tickets);

        Assert.Single(result);
        Assert.Single(result[0].Boards);
    }

    private static JiraTicket MakeTicket(string key, List<BoardRef> boards) => new()
    {
        Key = key,
        Summary = $"Test ticket {key}",
        Status = "To Do",
        StatusCategoryKey = "new",
        NormalizedCategory = NormalizedCategory.ToDo,
        Priority = "Medium",
        PriorityOrder = 3,
        Updated = DateTimeOffset.UtcNow,
        Created = DateTimeOffset.UtcNow,
        Reporter = "reporter",
        Assignee = "assignee",
        AssigneeAccountId = "acc-123",
        ProjectKey = key.Split('-')[0],
        ProjectName = "Test Project",
        Boards = boards
    };
}
