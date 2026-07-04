using System.Text.Json;
using Jmap.Json;

namespace Jmap.Core.Tests;

public class WebSocketMessageTests
{
    [Fact]
    public void Request_messages_carry_the_type_marker_and_correlation_id()
    {
        var json = WebSocketMessages.Request(new JmapRequest
        {
            Using = [JmapCapabilities.Core],
            MethodCalls = [Invocation.Create(CoreMethods.CoreEcho, new { hello = true }, "c0")],
        }, "r5");

        Assert.Contains("\"@type\":\"Request\"", json);
        Assert.Contains("\"id\":\"r5\"", json);
        Assert.Contains("\"methodCalls\":[[\"Core/echo\",{\"hello\":true},\"c0\"]]", json);
    }

    [Fact]
    public void Push_enable_writes_null_data_types_explicitly()
    {
        Assert.Equal(
            """{"@type":"WebSocketPushEnable","dataTypes":null}""",
            WebSocketMessages.PushEnable(null, null));

        Assert.Equal(
            """{"@type":"WebSocketPushEnable","dataTypes":["Email","Mailbox"],"pushState":"aaa"}""",
            WebSocketMessages.PushEnable(["Email", "Mailbox"], "aaa"));

        Assert.Equal("""{"@type":"WebSocketPushDisable"}""", WebSocketMessages.PushDisable());
    }

    [Theory]
    [InlineData("""{"@type":"Response","requestId":"r1","methodResponses":[],"sessionState":"s"}""", "Response", "r1")]
    [InlineData("""{"@type":"RequestError","requestId":"r2","type":"urn:ietf:params:jmap:error:notJSON","status":400}""", "RequestError", "r2")]
    [InlineData("""{"@type":"RequestError","requestId":null,"type":"urn:ietf:params:jmap:error:notJSON"}""", "RequestError", null)]
    [InlineData("""{"@type":"StateChange","changed":{}}""", "StateChange", null)]
    [InlineData("""{"hello":"world"}""", "Unknown", null)]
    public void Classify_dispatches_on_the_type_marker(string json, string expectedKind, string? expectedRequestId)
    {
        using var document = JsonDocument.Parse(json);

        var kind = WebSocketMessages.Classify(document.RootElement, out var requestId);

        Assert.Equal(Enum.Parse<WebSocketMessages.Kind>(expectedKind), kind);
        Assert.Equal(expectedRequestId, requestId);
    }

    [Fact]
    public void State_change_over_websocket_carries_the_push_state()
    {
        const string json =
            """
            {"@type":"StateChange",
             "changed":{"a3123":{"Email":"d35ecb040aab","Mailbox":"d35ecb040aab"}},
             "pushState":"aaa"}
            """;

        var change = JsonSerializer.Deserialize<StateChange>(json, JmapJson.Options)!;

        Assert.Equal("aaa", change.PushState);
        Assert.Equal("d35ecb040aab", change.Changed["a3123"]["Email"]);
    }
}
