namespace Crosswalk.Core.Jira;

public class JiraApiException(string message) : Exception(message);

public class JiraAuthException(string message) : JiraApiException(message);

public class JiraRateLimitException(TimeSpan retryAfter, string message) : JiraApiException(message)
{
    public TimeSpan RetryAfter { get; } = retryAfter;
}
