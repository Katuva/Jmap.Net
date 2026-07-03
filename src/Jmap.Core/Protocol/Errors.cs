using System.Text.Json.Serialization;

namespace Jmap;

/// <summary>A method-level error (RFC 8620 §3.6.2), e.g. "unknownMethod", "invalidArguments".</summary>
public sealed record JmapMethodError
{
    public required string Type { get; init; }

    public string? Description { get; init; }
}

/// <summary>An HTTP request-level error, an RFC 7807 problem details object (RFC 8620 §3.6.1).</summary>
public sealed record JmapProblemDetails
{
    public string? Type { get; init; }

    public int? Status { get; init; }

    public string? Detail { get; init; }

    /// <summary>For "limit" problems: which server limit was exceeded.</summary>
    public string? Limit { get; init; }
}

/// <summary>Base for everything this library throws deliberately.</summary>
public class JmapException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>The server rejected the whole request (HTTP-level, RFC 8620 §3.6.1).</summary>
public sealed class JmapRequestException(JmapProblemDetails problem)
    : JmapException($"JMAP request failed: {problem.Type ?? "unknown"} ({problem.Status}) {problem.Detail}".TrimEnd())
{
    public JmapProblemDetails Problem { get; } = problem;
}

/// <summary>One method call in the request failed (RFC 8620 §3.6.2).</summary>
public sealed class JmapMethodException(string callId, JmapMethodError error)
    : JmapException($"JMAP method call '{callId}' failed: {error.Type}{(error.Description is null ? "" : $" — {error.Description}")}")
{
    public string CallId { get; } = callId;

    public JmapMethodError Error { get; } = error;
}

/// <summary>Well-known method-level error types (RFC 8620 §3.6.2).</summary>
public static class JmapErrorTypes
{
    public const string ServerUnavailable = "serverUnavailable";
    public const string ServerFail = "serverFail";
    public const string ServerPartialFail = "serverPartialFail";
    public const string UnknownMethod = "unknownMethod";
    public const string InvalidArguments = "invalidArguments";
    public const string InvalidResultReference = "invalidResultReference";
    public const string Forbidden = "forbidden";
    public const string AccountNotFound = "accountNotFound";
    public const string AccountNotSupportedByMethod = "accountNotSupportedByMethod";
    public const string AccountReadOnly = "accountReadOnly";
    public const string RequestTooLarge = "requestTooLarge";
    public const string CannotCalculateChanges = "cannotCalculateChanges";
    public const string StateMismatch = "stateMismatch";
}
