using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Jmap.Json;

namespace Jmap;

/// <summary>
/// A JMAP-over-WebSocket connection (RFC 8887), opened via
/// <see cref="JmapClient.ConnectWebSocketAsync"/>. Requests share one socket and are
/// correlated by id, so several may be in flight at once; when the server supports push,
/// <see cref="EnablePushAsync"/> makes <see cref="GetEventsAsync"/> yield state changes
/// without a separate EventSource connection. Reconnection is the caller's job.
/// </summary>
public sealed class JmapWebSocketConnection : IAsyncDisposable
{
    private readonly WebSocket _socket;
    private readonly CancellationTokenSource _closed = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JmapResponse>> _pending = new();
    private readonly Channel<StateChange> _stateChanges = Channel.CreateUnbounded<StateChange>(
        new UnboundedChannelOptions { SingleWriter = true });
    private readonly Task _receiveLoop;
    private int _requestCounter;
    private bool _disposed;

    internal JmapWebSocketConnection(WebSocket socket, bool supportsPush)
    {
        _socket = socket;
        SupportsPush = supportsPush;
        _receiveLoop = Task.Run(ReceiveLoopAsync);
    }

    /// <summary>From the session's websocket capability: whether push may be enabled here.</summary>
    public bool SupportsPush { get; }

    /// <summary>Executes one API request over the socket and awaits its correlated response.</summary>
    public async Task<JmapResponse> SendAsync(JmapRequest request, CancellationToken cancellationToken = default)
    {
        var id = "r" + Interlocked.Increment(ref _requestCounter);
        var pending = new TaskCompletionSource<JmapResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = pending;
        try
        {
            await SendTextAsync(WebSocketMessages.Request(request, id), cancellationToken);
            using var registration = cancellationToken.Register(() => pending.TrySetCanceled(cancellationToken));
            return await pending.Task;
        }
        finally
        {
            _pending.TryRemove(id, out _);
        }
    }

    /// <summary>
    /// Asks the server to push state changes on this connection (RFC 8887 §4.3.5.2).
    /// Null dataTypes means all types. Passing the pushState from a previous connection's
    /// last <see cref="StateChange"/> makes the server send everything missed since.
    /// </summary>
    public Task EnablePushAsync(
        IReadOnlyList<string>? dataTypes = null, string? pushState = null, CancellationToken cancellationToken = default)
        => SendTextAsync(WebSocketMessages.PushEnable(dataTypes, pushState), cancellationToken);

    public Task DisablePushAsync(CancellationToken cancellationToken = default)
        => SendTextAsync(WebSocketMessages.PushDisable(), cancellationToken);

    /// <summary>
    /// The state changes pushed on this connection, until it closes or is cancelled. The
    /// sequence may be consumed once; enumeration ends normally when the connection closes.
    /// </summary>
    public IAsyncEnumerable<StateChange> GetEventsAsync(CancellationToken cancellationToken = default)
        => _stateChanges.Reader.ReadAllAsync(cancellationToken);

    private async Task SendTextAsync(string message, CancellationToken cancellationToken)
    {
        var payload = Encoding.UTF8.GetBytes(message);
        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            await _socket.SendAsync(payload, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task ReceiveLoopAsync()
    {
        var buffer = new byte[16 * 1024];
        var message = new MemoryStream();
        try
        {
            while (true)
            {
                message.SetLength(0);
                WebSocketReceiveResult result;
                do
                {
                    result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), _closed.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Complete(transportError: null); // graceful close: events end, requests fail
                        return;
                    }

                    message.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    HandleMessage(Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length));
                }
            }
        }
        catch (OperationCanceledException)
        {
            Complete(transportError: null); // local close via DisposeAsync
        }
        catch (Exception exception)
        {
            Complete(new JmapException("The WebSocket connection failed.", exception));
        }
    }

    private void HandleMessage(string json)
    {
        using var document = JsonDocument.Parse(json);
        switch (WebSocketMessages.Classify(document.RootElement, out var requestId))
        {
            case WebSocketMessages.Kind.Response:
                if (requestId is not null && _pending.TryGetValue(requestId, out var pending))
                {
                    pending.TrySetResult(document.RootElement.Deserialize<JmapResponse>(JmapJson.Options)!);
                }

                break;

            case WebSocketMessages.Kind.RequestError:
                var problem = document.RootElement.Deserialize<JmapProblemDetails>(JmapJson.Options)!;
                if (requestId is not null && _pending.TryGetValue(requestId, out var failed))
                {
                    failed.TrySetException(new JmapRequestException(problem));
                }

                break;

            case WebSocketMessages.Kind.StateChange:
                if (document.RootElement.Deserialize<StateChange>(JmapJson.Options) is { } change)
                {
                    _stateChanges.Writer.TryWrite(change);
                }

                break;
        }
    }

    /// <summary>Ends the events stream (with an error only for transport failures, so a
    /// clean close ends enumeration normally) and fails any in-flight requests.</summary>
    private void Complete(Exception? transportError)
    {
        _stateChanges.Writer.TryComplete(transportError);
        foreach (var (id, pending) in _pending)
        {
            pending.TrySetException(transportError ?? new JmapException("The WebSocket connection was closed."));
            _pending.TryRemove(id, out _);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _closed.Cancel();
        if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try
            {
                using var closeTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", closeTimeout.Token);
            }
            catch
            {
                // Best-effort close; the socket is being torn down regardless.
            }
        }

        try
        {
            await _receiveLoop;
        }
        catch
        {
            // Failures already surfaced through Complete().
        }

        _socket.Dispose();
        _closed.Dispose();
        _sendLock.Dispose();
    }
}
