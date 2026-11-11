using Crosswalk.Core.Sync;

namespace Crosswalk.Tests;

public class JiraUrlNormalizerTests
{
    [Theory]
    [InlineData("https://harborline.atlassian.net", "https://harborline.atlassian.net")]
    [InlineData("https://harborline.atlassian.net/", "https://harborline.atlassian.net")]
    [InlineData("https://harborline.atlassian.net/browse/CLM-123", "https://harborline.atlassian.net")]
    [InlineData("https://HARBORLINE.atlassian.net/jira/software/projects/CLM/boards/42", "https://harborline.atlassian.net")]
    [InlineData("http://harborline.atlassian.net", "https://harborline.atlassian.net")]
    [InlineData("harborline.atlassian.net", "https://harborline.atlassian.net")]
    [InlineData("harborline", "https://harborline.atlassian.net")]
    [InlineData("  harborline  ", "https://harborline.atlassian.net")]
    public void Normalize_ValidInputs(string input, string expected)
    {
        Assert.Equal(expected, JiraUrlNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a valid url with spaces")]
    public void Normalize_InvalidInputs_ReturnsNull(string? input)
    {
        Assert.Null(JiraUrlNormalizer.Normalize(input));
    }

    [Fact]
    public void ExtractSiteName_Works()
    {
        Assert.Equal("harborline", JiraUrlNormalizer.ExtractSiteName("https://harborline.atlassian.net"));
    }

    [Fact]
    public void BuildTicketUrl_Works()
    {
        Assert.Equal(
            "https://harborline.atlassian.net/browse/CLM-123",
            JiraUrlNormalizer.BuildTicketUrl("https://harborline.atlassian.net", "CLM-123"));
    }
}
