using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Jmap.Json;

namespace Jmap;

/// <summary>
/// A JMAP client over HTTP (RFC 8620): fetches the session, executes API requests, moves
/// blobs, and listens for push events over the session's EventSource endpoint.
/// </summary>
public sealed class JmapClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;

    public JmapClient(JmapClientOptions options, HttpClient? httpClient = null)
    {
        Options = options;
        _ownsHttp = httpClient is null;
        _http = httpClient ?? new HttpClient();
        if (options.BuildAuthorization() is { } authorization)
        {
            _http.DefaultRequestHeaders.Authorization = authorization;
        }
    }

    public JmapClientOptions Options { get; }

    /// <summary>The last-fetched session, once <see cref="ConnectAsync"/> has run.</summary>
    public JmapSession? Session { get; private set; }

    /// <summary>The session state carried on the most recent API response.</summary>
    public string? LatestSessionState { get; private set; }

    /// <summary>Fetches (or re-fetches) the session resource.</summary>
    public async Task<JmapSession> ConnectAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync(Options.SessionUrl, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        Session = await response.Content.ReadFromJsonAsync<JmapSession>(JmapJson.Options, cancellationToken)
            ?? throw new JmapException("The session resource was empty.");
        return Session;
    }

    /// <summary>Executes one API request (a batch of method calls) against the session's apiUrl.</summary>
    public async Task<JmapResponse> SendAsync(JmapRequest request, CancellationToken cancellationToken = default)
    {
        var session = await RequireSessionAsync(cancellationToken);
        using var response = await _http.PostAsJsonAsync(session.ApiUrl, request, JmapJson.Options, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var parsed = await response.Content.ReadFromJsonAsync<JmapResponse>(JmapJson.Options, cancellationToken)
            ?? throw new JmapException("The API response was empty.");
        LatestSessionState = parsed.SessionState;
        return parsed;
    }

    /// <summary>Convenience: one method call, returning its typed response.</summary>
    public async Task<TResponse> CallAsync<TResponse>(
        string method, object arguments, IReadOnlyList<string> usingCapabilities, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(new JmapRequest
        {
            Using = usingCapabilities,
            MethodCalls = [Invocation.Create(method, arguments, "c0")],
        }, cancellationToken);
        return response.Require<TResponse>("c0");
    }

    /// <summary>Uploads a blob (RFC 8620 §6.1) and returns its id for use in method calls.</summary>
    public async Task<BlobInfo> UploadBlobAsync(
        string accountId, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var session = await RequireSessionAsync(cancellationToken);
        var url = ExpandTemplate(session.UploadUrl, ("accountId", accountId));

        using var body = new StreamContent(content);
        body.Headers.TryAddWithoutValidation("Content-Type", contentType);
        using var response = await _http.PostAsync(url, body, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<BlobInfo>(JmapJson.Options, cancellationToken)
            ?? throw new JmapException("The upload response was empty.");
    }

    /// <summary>Downloads a blob (RFC 8620 §6.2). Dispose the stream when done.</summary>
    public async Task<Stream> DownloadBlobAsync(
        string accountId, string blobId, string type = "application/octet-stream", string name = "blob",
        CancellationToken cancellationToken = default)
    {
        var session = await RequireSessionAsync(cancellationToken);
        var url = ExpandTemplate(session.DownloadUrl,
            ("accountId", accountId), ("blobId", blobId), ("type", type), ("name", name));

        var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        try
        {
            await EnsureSuccessAsync(response, cancellationToken);
            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Listens for push notifications on the session's EventSource endpoint (RFC 8620 §7.3),
    /// yielding a <see cref="StateChange"/> per event until cancelled or the server closes
    /// the stream. Callers reconnect (with backoff) as needed.
    /// </summary>
    public async IAsyncEnumerable<StateChange> GetEventsAsync(
        IReadOnlyList<string>? types = null, int pingSeconds = 60,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var session = await RequireSessionAsync(cancellationToken);
        var url = ExpandTemplate(session.EventSourceUrl,
            ("types", types is null ? "*" : string.Join(',', types)),
            ("closeafter", "no"),
            ("ping", pingSeconds.ToString()));

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Accept", "text/event-stream");
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        await foreach (var data in EventSourceParser.ReadEventsAsync(reader, "state", cancellationToken))
        {
            if (JsonSerializer.Deserialize<StateChange>(data, JmapJson.Options) is { } change)
            {
                yield return change;
            }
        }
    }

    public void Dispose()
    {
        if (_ownsHttp)
        {
            _http.Dispose();
        }
    }

    private async ValueTask<JmapSession> RequireSessionAsync(CancellationToken cancellationToken)
        => Session ?? await ConnectAsync(cancellationToken);

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        JmapProblemDetails? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<JmapProblemDetails>(JmapJson.Options, cancellationToken);
        }
        catch (JsonException)
        {
            // Not a problem-details body; fall through to a status-only error.
        }

        throw new JmapRequestException(problem ?? new JmapProblemDetails { Status = (int)response.StatusCode });
    }

    // RFC 6570 level-1 expansion — the only level the session's URI templates use.
    internal static Uri ExpandTemplate(string template, params (string Name, string Value)[] variables)
    {
        var expanded = template;
        foreach (var (name, value) in variables)
        {
            expanded = expanded.Replace("{" + name + "}", Uri.EscapeDataString(value));
        }

        return new Uri(expanded);
    }
}
