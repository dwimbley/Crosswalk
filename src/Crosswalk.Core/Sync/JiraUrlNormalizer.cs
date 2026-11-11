using System.Text.RegularExpressions;

namespace Crosswalk.Core.Sync;

public static partial class JiraUrlNormalizer
{
    /// <summary>
    /// Normalizes any pasted Jira Cloud URL to https://{site}.atlassian.net
    /// Accepts:
    ///   - https://foo.atlassian.net
    ///   - https://foo.atlassian.net/browse/PROJ-123
    ///   - foo.atlassian.net
    ///   - foo
    /// </summary>
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        input = input.Trim().TrimEnd('/');

        // Try to extract the site name from a full URL
        var match = AtlassianUrlPattern().Match(input);
        if (match.Success)
        {
            var siteName = match.Groups["site"].Value.ToLowerInvariant();
            return $"https://{siteName}.atlassian.net";
        }

        // Try bare hostname: "foo.atlassian.net"
        var hostMatch = BareHostPattern().Match(input);
        if (hostMatch.Success)
        {
            var siteName = hostMatch.Groups["site"].Value.ToLowerInvariant();
            return $"https://{siteName}.atlassian.net";
        }

        // Try bare site name: "foo"
        if (SiteNameOnly().IsMatch(input))
        {
            return $"https://{input.ToLowerInvariant()}.atlassian.net";
        }

        return null;
    }

    /// <summary>
    /// Extracts the site name (subdomain) from a normalized base URL.
    /// </summary>
    public static string? ExtractSiteName(string baseUrl)
    {
        var match = AtlassianUrlPattern().Match(baseUrl);
        return match.Success ? match.Groups["site"].Value.ToLowerInvariant() : null;
    }

    /// <summary>
    /// Builds the browse URL for a ticket.
    /// </summary>
    public static string BuildTicketUrl(string baseUrl, string issueKey)
        => $"{baseUrl.TrimEnd('/')}/browse/{issueKey}";

    [GeneratedRegex(@"https?://(?<site>[a-zA-Z0-9\-]+)\.atlassian\.net", RegexOptions.IgnoreCase)]
    private static partial Regex AtlassianUrlPattern();

    [GeneratedRegex(@"^(?<site>[a-zA-Z0-9\-]+)\.atlassian\.net$", RegexOptions.IgnoreCase)]
    private static partial Regex BareHostPattern();

    [GeneratedRegex(@"^[a-zA-Z0-9\-]+$")]
    private static partial Regex SiteNameOnly();
}
