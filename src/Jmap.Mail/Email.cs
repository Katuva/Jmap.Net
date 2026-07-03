using System.Text.Json.Serialization;
using Jmap.Json;

namespace Jmap.Mail;

/// <summary>An email (RFC 8621 §4.1). All properties are optional on the wire — a /get
/// returns only the requested ones — so everything here is nullable or defaulted.</summary>
public sealed record Email
{
    // Metadata (§4.1.1)
    public string? Id { get; init; }
    public string? BlobId { get; init; }
    public string? ThreadId { get; init; }
    /// <summary>Mailbox id → true. An email is "in" every mailbox with a true entry.</summary>
    public IReadOnlyDictionary<string, bool>? MailboxIds { get; init; }
    /// <summary>Keyword → true; see <see cref="EmailKeywords"/> for the registered ones.</summary>
    public IReadOnlyDictionary<string, bool>? Keywords { get; init; }
    public long? Size { get; init; }
    [JsonConverter(typeof(UtcDateConverter))]
    public DateTimeOffset? ReceivedAt { get; init; }

    // Header-derived (§4.1.2–4.1.3)
    public IReadOnlyList<EmailHeader>? Headers { get; init; }
    public IReadOnlyList<string>? MessageId { get; init; }
    public IReadOnlyList<string>? InReplyTo { get; init; }
    public IReadOnlyList<string>? References { get; init; }
    public IReadOnlyList<EmailAddress>? Sender { get; init; }
    public IReadOnlyList<EmailAddress>? From { get; init; }
    public IReadOnlyList<EmailAddress>? To { get; init; }
    public IReadOnlyList<EmailAddress>? Cc { get; init; }
    public IReadOnlyList<EmailAddress>? Bcc { get; init; }
    public IReadOnlyList<EmailAddress>? ReplyTo { get; init; }
    public string? Subject { get; init; }
    /// <summary>The Date header — keeps its original zone offset (a "Date", not a "UTCDate").</summary>
    public DateTimeOffset? SentAt { get; init; }

    // Body (§4.1.4)
    public EmailBodyPart? BodyStructure { get; init; }
    public IReadOnlyDictionary<string, EmailBodyValue>? BodyValues { get; init; }
    public IReadOnlyList<EmailBodyPart>? TextBody { get; init; }
    public IReadOnlyList<EmailBodyPart>? HtmlBody { get; init; }
    public IReadOnlyList<EmailBodyPart>? Attachments { get; init; }
    public bool? HasAttachment { get; init; }
    public string? Preview { get; init; }
}

/// <summary>A parsed mailbox address (RFC 8621 §4.1.2.3).</summary>
public sealed record EmailAddress(string? Name, string Email);

/// <summary>A named address group (RFC 8621 §4.1.2.4).</summary>
public sealed record EmailAddressGroup(string? Name, IReadOnlyList<EmailAddress> Addresses);

/// <summary>A raw header field (RFC 8621 §4.1.2.1).</summary>
public sealed record EmailHeader(string Name, string Value);

/// <summary>One node of a message's MIME structure (RFC 8621 §4.1.4).</summary>
public sealed record EmailBodyPart
{
    public string? PartId { get; init; }
    public string? BlobId { get; init; }
    public long? Size { get; init; }
    public IReadOnlyList<EmailHeader>? Headers { get; init; }
    public string? Name { get; init; }
    public string? Type { get; init; }
    public string? Charset { get; init; }
    public string? Disposition { get; init; }
    /// <summary>The Content-Id, without angle brackets — matches cid: URLs in HTML bodies.</summary>
    public string? Cid { get; init; }
    public IReadOnlyList<string>? Language { get; init; }
    public string? Location { get; init; }
    public IReadOnlyList<EmailBodyPart>? SubParts { get; init; }
}

/// <summary>Decoded text of one body part (RFC 8621 §4.1.4).</summary>
public sealed record EmailBodyValue
{
    public required string Value { get; init; }
    public bool IsEncodingProblem { get; init; }
    public bool IsTruncated { get; init; }
}

/// <summary>The registered IMAP-compatible keywords (RFC 8621 §4.1.1).</summary>
public static class EmailKeywords
{
    public const string Draft = "$draft";
    public const string Seen = "$seen";
    public const string Flagged = "$flagged";
    public const string Answered = "$answered";
    public const string Forwarded = "$forwarded";
    public const string Phishing = "$phishing";
    public const string Junk = "$junk";
    public const string NotJunk = "$notjunk";
}
