using System.Text.Json.Serialization;
using Jmap.Json;

namespace Jmap.Mail;

/// <summary>An Email/query filter condition (RFC 8621 §4.4.1). Combine with
/// <see cref="JmapFilter.And"/>/<see cref="JmapFilter.Or"/>/<see cref="JmapFilter.Not"/>.</summary>
public sealed record EmailFilterCondition : JmapFilter
{
    public string? InMailbox { get; init; }
    public IReadOnlyList<string>? InMailboxOtherThan { get; init; }
    [JsonConverter(typeof(UtcDateConverter))]
    public DateTimeOffset? Before { get; init; }
    [JsonConverter(typeof(UtcDateConverter))]
    public DateTimeOffset? After { get; init; }
    public long? MinSize { get; init; }
    public long? MaxSize { get; init; }
    public string? AllInThreadHaveKeyword { get; init; }
    public string? SomeInThreadHaveKeyword { get; init; }
    public string? NoneInThreadHaveKeyword { get; init; }
    public string? HasKeyword { get; init; }
    public string? NotKeyword { get; init; }
    public bool? HasAttachment { get; init; }
    public string? Text { get; init; }
    public string? From { get; init; }
    public string? To { get; init; }
    public string? Cc { get; init; }
    public string? Bcc { get; init; }
    public string? Subject { get; init; }
    public string? Body { get; init; }
    /// <summary>[headerName] to test presence, or [headerName, value] to match a value.</summary>
    public IReadOnlyList<string>? Header { get; init; }
}

/// <summary>A Mailbox/query filter condition (RFC 8621 §2.3). Note: filtering for a null
/// parentId (top-level mailboxes) can't be expressed here because absent and null are
/// serialised identically; query all and filter client-side for that case.</summary>
public sealed record MailboxFilterCondition : JmapFilter
{
    public string? ParentId { get; init; }
    public string? Name { get; init; }
    public string? Role { get; init; }
    public bool? HasAnyRole { get; init; }
    public bool? IsSubscribed { get; init; }
}

/// <summary>An EmailSubmission/query filter condition (RFC 8621 §7.3).</summary>
public sealed record EmailSubmissionFilterCondition : JmapFilter
{
    public IReadOnlyList<string>? IdentityIds { get; init; }
    public IReadOnlyList<string>? EmailIds { get; init; }
    public IReadOnlyList<string>? ThreadIds { get; init; }
    public string? UndoStatus { get; init; }
    [JsonConverter(typeof(UtcDateConverter))]
    public DateTimeOffset? Before { get; init; }
    [JsonConverter(typeof(UtcDateConverter))]
    public DateTimeOffset? After { get; init; }
}
