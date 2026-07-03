namespace Jmap.Mail;

/// <summary>A sending identity (RFC 8621 §6): an address the user may send from, with signatures.</summary>
public sealed record Identity
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public required string Email { get; init; }
    public IReadOnlyList<EmailAddress>? ReplyTo { get; init; }
    public IReadOnlyList<EmailAddress>? Bcc { get; init; }
    public string? TextSignature { get; init; }
    public string? HtmlSignature { get; init; }
    public bool MayDelete { get; init; }
}
