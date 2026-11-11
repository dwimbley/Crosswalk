using Crosswalk.Core.Models;
using Crosswalk.Core.Sync;

namespace Crosswalk.Tests;

public class StatusNormalizerTests
{
    [Theory]
    [InlineData("To Do", "new", NormalizedCategory.ToDo)]
    [InlineData("Open", "new", NormalizedCategory.ToDo)]
    [InlineData("Backlog", "new", NormalizedCategory.ToDo)]
    [InlineData("Selected for Development", "new", NormalizedCategory.ToDo)]
    public void JiraStatusCategory_New_MapsToDo(string status, string categoryKey, NormalizedCategory expected)
    {
        var result = StatusNormalizer.Normalize(status, categoryKey);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("In Progress", "indeterminate", NormalizedCategory.InProgress)]
    [InlineData("In Development", "indeterminate", NormalizedCategory.InProgress)]
    [InlineData("Doing", "indeterminate", NormalizedCategory.InProgress)]
    public void JiraStatusCategory_Indeterminate_MapsInProgress(string status, string categoryKey, NormalizedCategory expected)
    {
        var result = StatusNormalizer.Normalize(status, categoryKey);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Done", "done", NormalizedCategory.Done)]
    [InlineData("Closed", "done", NormalizedCategory.Done)]
    [InlineData("Resolved", "done", NormalizedCategory.Done)]
    public void JiraStatusCategory_Done_MapsDone(string status, string categoryKey, NormalizedCategory expected)
    {
        var result = StatusNormalizer.Normalize(status, categoryKey);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("In Review", "indeterminate", NormalizedCategory.InReview)]
    [InlineData("Code Review", "indeterminate", NormalizedCategory.InReview)]
    [InlineData("QA", "indeterminate", NormalizedCategory.InReview)]
    [InlineData("QA Verify", "indeterminate", NormalizedCategory.InReview)]
    [InlineData("Verification", "indeterminate", NormalizedCategory.InReview)]
    public void NameHeuristics_Review_MapsInReview(string status, string categoryKey, NormalizedCategory expected)
    {
        var result = StatusNormalizer.Normalize(status, categoryKey);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Blocked", "indeterminate", NormalizedCategory.Blocked)]
    [InlineData("On Hold", "indeterminate", NormalizedCategory.Blocked)]
    [InlineData("Waiting", "indeterminate", NormalizedCategory.Blocked)]
    [InlineData("Blocked – Client Input", "indeterminate", NormalizedCategory.Blocked)]
    [InlineData("Impediment", "indeterminate", NormalizedCategory.Blocked)]
    public void NameHeuristics_Blocked_MapsBlocked(string status, string categoryKey, NormalizedCategory expected)
    {
        var result = StatusNormalizer.Normalize(status, categoryKey);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConnectionOverride_TakesPriority()
    {
        var map = new Dictionary<string, string> { { "Waiting", "blocked" } };
        var result = StatusNormalizer.Normalize("Waiting", "new", map);
        Assert.Equal(NormalizedCategory.Blocked, result);
    }

    [Fact]
    public void ConnectionOverride_OverridesHeuristic()
    {
        // "QA" would normally be InReview by heuristic, but override says InProgress
        var map = new Dictionary<string, string> { { "QA", "in_progress" } };
        var result = StatusNormalizer.Normalize("QA", "indeterminate", map);
        Assert.Equal(NormalizedCategory.InProgress, result);
    }

    [Fact]
    public void UnknownCategory_DefaultsToDo()
    {
        var result = StatusNormalizer.Normalize("Mystery Status", "unknown_category");
        Assert.Equal(NormalizedCategory.ToDo, result);
    }
}
