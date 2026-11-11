using System.Net;

namespace Crosswalk.Tests;

/// <summary>
/// A mock HTTP handler that returns pre-configured responses based on request URL patterns.
/// Used for integration testing the Jira client against recorded fixtures.
/// </summary>
public sealed class MockHttpHandler : HttpMessageHandler
{
    private readonly List<(Func<HttpRequestMessage, bool> Matcher, Func<HttpRequestMessage, HttpResponseMessage> Handler)> _handlers = [];

    public List<HttpRequestMessage> SentRequests { get; } = [];

    public void AddResponse(Func<HttpRequestMessage, bool> matcher, HttpStatusCode status, string body)
    {
        _handlers.Add((matcher, _ => new HttpResponseMessage(status)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        }));
    }

    public void AddResponse(string urlContains, HttpStatusCode status, string body)
    {
        AddResponse(
            req => req.RequestUri?.ToString().Contains(urlContains) == true,
            status,
            body);
    }

    public void AddSequentialResponses(string urlContains, params (HttpStatusCode Status, string Body)[] responses)
    {
        var index = 0;
        _handlers.Add((
            req => req.RequestUri?.ToString().Contains(urlContains) == true,
            _ =>
            {
                var (status, body) = responses[Math.Min(index++, responses.Length - 1)];
                return new HttpResponseMessage(status)
                {
                    Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
                };
            }
        ));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        SentRequests.Add(request);

        foreach (var (matcher, handler) in _handlers)
        {
            if (matcher(request))
                return Task.FromResult(handler(request));
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent($"No mock configured for {request.Method} {request.RequestUri}")
        });
    }
}
