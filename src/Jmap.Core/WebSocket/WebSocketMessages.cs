using System.Text.Json;
using System.Text.Json.Nodes;
using Jmap.Json;

namespace Jmap;

/// <summary>Builds and classifies the JSON messages RFC 8887 layers over the socket: every
/// message is a single object discriminated by its "@type" property.</summary>
internal static class WebSocketMessages
{
    public enum Kind
    {
        Unknown,
        Response,
        RequestError,
        StateChange,
    }

    /// <summary>A JMAP request as a WebSocket message (RFC 8887 §4.3.2): the request object
    /// plus <c>"@type": "Request"</c> and the id responses are correlated by.</summary>
    public static string Request(JmapRequest request, string id)
    {
        var node = JsonSerializer.SerializeToNode(request, JmapJson.Options)!.AsObject();
        node["@type"] = "Request";
        node["id"] = id;
        return node.ToJsonString(JmapJson.Options);
    }

    /// <summary>WebSocketPushEnable (RFC 8887 §4.3.5.2). dataTypes is meaningfully nullable
    /// (null = all types), so it is written explicitly rather than omitted.</summary>
    public static string PushEnable(IReadOnlyList<string>? dataTypes, string? pushState)
    {
        var node = new JsonObject
        {
            ["@type"] = "WebSocketPushEnable",
            ["dataTypes"] = dataTypes is null ? null : JsonSerializer.SerializeToNode(dataTypes, JmapJson.Options),
        };
        if (pushState is not null)
        {
            node["pushState"] = pushState;
        }

        return node.ToJsonString(JmapJson.Options);
    }

    /// <summary>WebSocketPushDisable (RFC 8887 §4.3.5.3).</summary>
    public static string PushDisable() => """{"@type":"WebSocketPushDisable"}""";

    /// <summary>Reads a server message's "@type" and, for request-scoped kinds, its requestId.</summary>
    public static Kind Classify(JsonElement message, out string? requestId)
    {
        requestId = null;
        if (message.ValueKind != JsonValueKind.Object
            || !message.TryGetProperty("@type", out var type)
            || type.ValueKind != JsonValueKind.String)
        {
            return Kind.Unknown;
        }

        var kind = type.GetString() switch
        {
            "Response" => Kind.Response,
            "RequestError" => Kind.RequestError,
            "StateChange" => Kind.StateChange,
            _ => Kind.Unknown,
        };

        if (kind is Kind.Response or Kind.RequestError
            && message.TryGetProperty("requestId", out var idElement)
            && idElement.ValueKind == JsonValueKind.String)
        {
            requestId = idElement.GetString();
        }

        return kind;
    }
}
