namespace Jmap;

/// <summary>The websocket capability object (RFC 8887 §3, urn:ietf:params:jmap:websocket).</summary>
public sealed record WebSocketCapability
{
    /// <summary>The wss:// URI to open the JMAP-over-WebSocket connection against.</summary>
    public required string Url { get; init; }

    /// <summary>Whether the server can push <see cref="StateChange"/>s over the socket.</summary>
    public bool SupportsPush { get; init; }
}
