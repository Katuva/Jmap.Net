namespace Jmap.Mail;

/// <summary>A mailbox/folder (RFC 8621 §2).</summary>
public sealed record Mailbox
{
    public string? Id { get; init; }
    public required string Name { get; init; }
    public string? ParentId { get; init; }
    /// <summary>Special-use role; see <see cref="MailboxRoles"/>. Null for ordinary folders.</summary>
    public string? Role { get; init; }
    public long SortOrder { get; init; }
    public long TotalEmails { get; init; }
    public long UnreadEmails { get; init; }
    public long TotalThreads { get; init; }
    public long UnreadThreads { get; init; }
    public MailboxRights? MyRights { get; init; }
    public bool IsSubscribed { get; init; }
}

/// <summary>What the user may do in a mailbox (RFC 8621 §2).</summary>
public sealed record MailboxRights
{
    public bool MayReadItems { get; init; }
    public bool MayAddItems { get; init; }
    public bool MayRemoveItems { get; init; }
    public bool MaySetSeen { get; init; }
    public bool MaySetKeywords { get; init; }
    public bool MayCreateChild { get; init; }
    public bool MayRename { get; init; }
    public bool MayDelete { get; init; }
    public bool MaySubmit { get; init; }
}

/// <summary>The registered mailbox roles (RFC 8621 §2 / IANA registry).</summary>
public static class MailboxRoles
{
    public const string Inbox = "inbox";
    public const string Archive = "archive";
    public const string Drafts = "drafts";
    public const string Sent = "sent";
    public const string Trash = "trash";
    public const string Junk = "junk";
    public const string All = "all";
    public const string Flagged = "flagged";
    public const string Important = "important";
    public const string Subscribed = "subscribed";
}
