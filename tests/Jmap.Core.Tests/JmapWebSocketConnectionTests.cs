using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Jmap.Core.Tests;

public class JmapWebSocketConnectionTests
{
    [Fact]
    public async Task Requests_correlate_by_id_and_state_changes_flow_to_the_events_stream()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var (client, server) = await ConnectPairAsync(timeout.Token);
        await using var connection = new JmapWebSocketConnection(client, supportsPush: true);

        var serverTask = Task.Run(async () =>
        {
            var message = await ReceiveTextAsync(server, timeout.Token);
            using var document = JsonDocument.Parse(message);
            Assert.Equal("Request", document.RootElement.GetProperty("@type").GetString());
            var id = document.RootElement.GetProperty("id").GetString();

            // A push arriving before the response must not confuse the correlation.
            await SendTextAsync(server,
                """{"@type":"StateChange","changed":{"A1":{"Email":"s99"}},"pushState":"p1"}""", timeout.Token);
            await SendTextAsync(server,
                $$"""{"@type":"Response","requestId":"{{id}}","methodResponses":[["Core/echo",{"hello":true},"c0"]],"sessionState":"s1"}""",
                timeout.Token);
        }, timeout.Token);

        var response = await connection.SendAsync(new JmapRequest
        {
            Using = [JmapCapabilities.Core],
            MethodCalls = [Invocation.Create(CoreMethods.CoreEcho, new { hello = true }, "c0")],
        }, timeout.Token);

        Assert.Equal("s1", response.SessionState);
        Assert.True(response.Require<JsonElement>("c0").GetProperty("hello").GetBoolean());

        await foreach (var change in connection.GetEventsAsync(timeout.Token))
        {
            Assert.Equal("p1", change.PushState);
            Assert.Equal("s99", change.Changed["A1"]["Email"]);
            break;
        }

        await serverTask;
        server.Dispose();
    }

    [Fact]
    public async Task Request_errors_fault_the_awaiting_call()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var (client, server) = await ConnectPairAsync(timeout.Token);
        await using var connection = new JmapWebSocketConnection(client, supportsPush: false);

        var serverTask = Task.Run(async () =>
        {
            var message = await ReceiveTextAsync(server, timeout.Token);
            using var document = JsonDocument.Parse(message);
            var id = document.RootElement.GetProperty("id").GetString();
            await SendTextAsync(server,
                $$"""{"@type":"RequestError","requestId":"{{id}}","type":"urn:ietf:params:jmap:error:limit","status":400,"limit":"maxSizeRequest"}""",
                timeout.Token);
        }, timeout.Token);

        var failure = await Assert.ThrowsAsync<JmapRequestException>(() => connection.SendAsync(new JmapRequest
        {
            Using = [JmapCapabilities.Core],
            MethodCalls = [Invocation.Create(CoreMethods.CoreEcho, new { }, "c0")],
        }, timeout.Token));

        Assert.Equal("maxSizeRequest", failure.Problem.Limit);
        await serverTask;
        server.Dispose();
    }

    // WebSocket.CreateFromStream over a loopback TCP pair: real sockets, no HTTP handshake.
    private static async Task<(WebSocket Client, WebSocket Server)> ConnectPairAsync(CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var clientTcp = new TcpClient();
            await clientTcp.ConnectAsync((IPEndPoint)listener.LocalEndpoint, cancellationToken);
            var serverTcp = await listener.AcceptTcpClientAsync(cancellationToken);
            return (
                WebSocket.CreateFromStream(clientTcp.GetStream(), new WebSocketCreationOptions { IsServer = false }),
                WebSocket.CreateFromStream(serverTcp.GetStream(), new WebSocketCreationOptions { IsServer = true }));
        }
        finally
        {
            listener.Stop();
        }
    }

    private static Task SendTextAsync(WebSocket socket, string message, CancellationToken cancellationToken)
        => socket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, endOfMessage: true, cancellationToken);

    private static async Task<string> ReceiveTextAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[64 * 1024];
        var length = 0;
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer, length, buffer.Length - length), cancellationToken);
            length += result.Count;
        } while (!result.EndOfMessage);

        return Encoding.UTF8.GetString(buffer, 0, length);
    }
}
