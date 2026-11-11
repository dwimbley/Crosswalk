using System.Net.Http.Headers;
using System.Text;

namespace Crosswalk.Core.Jira;

/// <summary>
/// DelegatingHandler that applies Basic auth (email:apiToken) to outgoing requests.
/// Designed to be created per-request or per-connection, not shared across connections.
/// </summary>
public sealed class JiraAuthHandler : DelegatingHandler
{
    private readonly string _email;
    private readonly string _apiToken;

    public JiraAuthHandler(string email, string apiToken)
        : base(new HttpClientHandler())
    {
        _email = email;
        _apiToken = apiToken;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_email}:{_apiToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Headers.UserAgent.ParseAdd("Crosswalk/1.0");
        request.Headers.Accept.ParseAdd("application/json");
        return base.SendAsync(request, cancellationToken);
    }
}
