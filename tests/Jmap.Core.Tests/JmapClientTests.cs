using System.Net;
using System.Text;

namespace Jmap.Core.Tests;

public class JmapClientTests
{
    /// <summary>Routes requests by URL prefix and records everything sent.</summary>
    private sealed class FakeHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string> Bodies { get; } = [];
        public Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } =
            _ => new HttpResponseMessage(HttpStatusCode.NotFound);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            Bodies.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
            return Respond(request);
        }
    }

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json"),
    };

    private static (JmapClient Client, FakeHandler Handler) Build()
    {
        var handler = new FakeHandler();
        var client = new JmapClient(
            new JmapClientOptions { SessionUrl = new Uri("https://jmap.example.com/session"), BearerToken = "tok-123" },
            new HttpClient(handler));
        return (client, handler);
    }

    [Fact]
    public async Task Connect_fetches_the_session_with_bearer_auth()
    {
        var (client, handler) = Build();
        handler.Respond = _ => Json(SessionTests.SessionJson);

        var session = await client.ConnectAsync();

        Assert.Equal("john@example.com", session.Username);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("Bearer tok-123", request.Headers.Authorization!.ToString());
    }

    [Fact]
    public async Task Send_posts_to_the_api_url_and_tracks_session_state()
    {
        var (client, handler) = Build();
        handler.Respond = request => request.RequestUri!.AbsolutePath.EndsWith("/session")
            ? Json(SessionTests.SessionJson)
            : Json("""{"methodResponses":[["Core/echo",{"hi":1},"c0"]],"sessionState":"s-next"}""");

        var response = await client.SendAsync(new JmapRequest
        {
            Using = [JmapCapabilities.Core],
            MethodCalls = [Invocation.Create("Core/echo", new { hi = 1 }, "c0")],
        });

        Assert.Equal("s-next", client.LatestSessionState);
        Assert.Single(response.MethodResponses);
        Assert.Equal(new Uri("https://jmap.example.com/api/"), handler.Requests[^1].RequestUri);
        Assert.Contains("\"Core/echo\"", handler.Bodies[^1]);
    }

    [Fact]
    public async Task Request_level_errors_surface_problem_details()
    {
        var (client, handler) = Build();
        handler.Respond = request => request.RequestUri!.AbsolutePath.EndsWith("/session")
            ? Json(SessionTests.SessionJson)
            : new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    """{"type":"urn:ietf:params:jmap:error:notRequest","status":400,"detail":"nope"}""",
                    Encoding.UTF8, "application/problem+json"),
            };

        var failure = await Assert.ThrowsAsync<JmapRequestException>(() => client.SendAsync(new JmapRequest
        {
            Using = [JmapCapabilities.Core],
            MethodCalls = [],
        }));

        Assert.Equal(400, failure.Problem.Status);
        Assert.Equal("nope", failure.Problem.Detail);
    }

    [Fact]
    public async Task Blob_urls_expand_their_templates()
    {
        var (client, handler) = Build();
        handler.Respond = request =>
        {
            var url = request.RequestUri!.AbsoluteUri;
            if (url.EndsWith("/session")) return Json(SessionTests.SessionJson);
            if (url.Contains("/upload/")) return Json("""{"accountId":"A13824","blobId":"B9","type":"text/plain","size":5}""");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) };
        };

        var uploaded = await client.UploadBlobAsync("A13824", new MemoryStream("hello"u8.ToArray()), "text/plain");
        Assert.Equal("B9", uploaded.BlobId);
        Assert.Equal("https://jmap.example.com/upload/A13824/", handler.Requests[^1].RequestUri!.AbsoluteUri);

        await using var download = await client.DownloadBlobAsync("A13824", "B9", "text/plain", "note.txt");
        Assert.Equal(
            "https://jmap.example.com/download/A13824/B9/note.txt?accept=text%2Fplain",
            handler.Requests[^1].RequestUri!.AbsoluteUri);
    }

    [Fact]
    public void Template_expansion_escapes_values()
    {
        var expanded = JmapClient.ExpandTemplate(
            "https://x.example/dl/{accountId}/{name}", ("accountId", "A/1"), ("name", "a b.txt"));

        Assert.Equal("https://x.example/dl/A%2F1/a%20b.txt", expanded.AbsoluteUri);
    }
}
