namespace Jmap;

/// <summary>A JMAP API request (RFC 8620 §3.3).</summary>
public sealed record JmapRequest
{
    /// <summary>Capability URNs the request uses (see <see cref="JmapCapabilities"/>).</summary>
    public required IReadOnlyList<string> Using { get; init; }

    public required IReadOnlyList<Invocation> MethodCalls { get; init; }

    /// <summary>Client-side creation-id → real-id map carried across requests (RFC 8620 §3.3).</summary>
    public IReadOnlyDictionary<string, string>? CreatedIds { get; init; }
}
