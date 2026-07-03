using System.Text.Json.Serialization;
using Jmap.Json;

namespace Jmap.Mail;

/// <summary>A message queued for sending (RFC 8621 §7) — JMAP's replacement for SMTP submission.</summary>
public sealed record EmailSubmission
{
    public string? Id { get; init; }
    public required string IdentityId { get; init; }
    public required string EmailId { get; init; }
    public string? ThreadId { get; init; }
    /// <summary>Null lets the server derive it from the message headers.</summary>
    public SubmissionEnvelope? Envelope { get; init; }
    [JsonConverter(typeof(UtcDateConverter))]
    public DateTimeOffset? SendAt { get; init; }
    /// <summary>"pending" | "final" | "canceled". Set to "canceled" to undo a scheduled send.</summary>
    public string? UndoStatus { get; init; }
    public IReadOnlyDictionary<string, DeliveryStatus>? DeliveryStatus { get; init; }
    public IReadOnlyList<string>? DsnBlobIds { get; init; }
    public IReadOnlyList<string>? MdnBlobIds { get; init; }
}

/// <summary>The SMTP envelope (RFC 8621 §7): return path and recipients.</summary>
public sealed record SubmissionEnvelope(SubmissionAddress MailFrom, IReadOnlyList<SubmissionAddress> RcptTo);

/// <summary>An envelope address with optional SMTP parameters (e.g. DSN NOTIFY).</summary>
public sealed record SubmissionAddress(string Email, IReadOnlyDictionary<string, string?>? Parameters = null);

/// <summary>Per-recipient delivery progress (RFC 8621 §7).</summary>
public sealed record DeliveryStatus
{
    public required string SmtpReply { get; init; }
    /// <summary>"queued" | "yes" | "no" | "unknown".</summary>
    public required string Delivered { get; init; }
    /// <summary>"unknown" | "yes" (from MDNs).</summary>
    public required string Displayed { get; init; }
}

/// <summary>An auto-reply configuration (RFC 8621 §8). Singleton: its id is always "singleton".</summary>
public sealed record VacationResponse
{
    public string Id { get; init; } = "singleton";
    public bool IsEnabled { get; init; }
    [JsonConverter(typeof(UtcDateConverter))]
    public DateTimeOffset? FromDate { get; init; }
    [JsonConverter(typeof(UtcDateConverter))]
    public DateTimeOffset? ToDate { get; init; }
    public string? Subject { get; init; }
    public string? TextBody { get; init; }
    public string? HtmlBody { get; init; }
}
