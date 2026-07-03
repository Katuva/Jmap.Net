namespace Jmap;

/// <summary>A JMAP API response (RFC 8620 §3.4).</summary>
public sealed record JmapResponse
{
    public required IReadOnlyList<Invocation> MethodResponses { get; init; }

    public IReadOnlyDictionary<string, string>? CreatedIds { get; init; }

    /// <summary>The session state at the time of this response; re-fetch the session when it moves.</summary>
    public required string SessionState { get; init; }

    /// <summary>All responses to a given method call (a single call may produce several).</summary>
    public IEnumerable<Invocation> ResponsesTo(string callId)
        => MethodResponses.Where(r => r.CallId == callId);

    /// <summary>
    /// The single response to a call, parsed as <typeparamref name="T"/>. Throws
    /// <see cref="JmapMethodException"/> when the server answered with a method-level error.
    /// </summary>
    public T Require<T>(string callId)
    {
        var invocation = MethodResponses.FirstOrDefault(r => r.CallId == callId)
            ?? throw new JmapException($"The response contains no answer to call '{callId}'.");
        return invocation.IsError ? throw new JmapMethodException(callId, invocation.AsError()) : invocation.ArgumentsAs<T>();
    }
}
