using System.Text.Json;
using Jmap.Json;

namespace Jmap.Mail.Tests;

public class EmailJsonTests
{
    [Fact]
    public void Deserialises_a_fetched_email_with_bodies()
    {
        const string json =
            """
            {
              "id": "M1", "blobId": "B1", "threadId": "T1",
              "mailboxIds": {"MB-inbox": true},
              "keywords": {"$seen": true, "$flagged": true},
              "size": 4096,
              "receivedAt": "2026-07-03T10:15:00Z",
              "messageId": ["m1@example.com"],
              "inReplyTo": ["m0@example.com"],
              "references": ["root@example.com", "m0@example.com"],
              "from": [{"name": "Sender", "email": "sender@example.com"}],
              "to": [{"name": null, "email": "you@example.com"}],
              "subject": "Re: the raven",
              "sentAt": "2026-07-03T12:15:00+02:00",
              "bodyValues": {"1": {"value": "remembers everything", "isTruncated": false}},
              "textBody": [{"partId": "1", "type": "text/plain", "size": 20}],
              "attachments": [{"partId": "3", "blobId": "B7", "type": "application/pdf", "name": "report.pdf", "size": 1234, "cid": null}],
              "hasAttachment": true,
              "preview": "remembers everything"
            }
            """;

        var email = JsonSerializer.Deserialize<Email>(json, JmapJson.Options)!;

        Assert.Equal("M1", email.Id);
        Assert.True(email.MailboxIds!["MB-inbox"]);
        Assert.True(email.Keywords!.ContainsKey(EmailKeywords.Seen));
        Assert.Equal(new DateTimeOffset(2026, 7, 3, 10, 15, 0, TimeSpan.Zero), email.ReceivedAt);
        Assert.Equal(TimeSpan.FromHours(2), email.SentAt!.Value.Offset); // Date keeps its zone
        Assert.Equal(["root@example.com", "m0@example.com"], email.References);
        Assert.Equal("remembers everything", email.BodyValues!["1"].Value);
        Assert.Equal("report.pdf", Assert.Single(email.Attachments!).Name);
        Assert.Equal("sender@example.com", Assert.Single(email.From!).Email);
    }

    [Fact]
    public void Serialises_a_draft_for_creation_without_nulls()
    {
        var draft = new Email
        {
            MailboxIds = new Dictionary<string, bool> { ["MB-drafts"] = true },
            Keywords = new Dictionary<string, bool> { [EmailKeywords.Draft] = true, [EmailKeywords.Seen] = true },
            From = [new EmailAddress("Me", "me@example.com")],
            To = [new EmailAddress(null, "you@example.com")],
            Subject = "wip",
            BodyValues = new Dictionary<string, EmailBodyValue> { ["1"] = new() { Value = "hello" } },
            TextBody = [new EmailBodyPart { PartId = "1", Type = "text/plain" }],
        };

        var json = JsonSerializer.Serialize(draft, JmapJson.Options);

        Assert.Contains("\"$draft\":true", json);
        Assert.Contains("\"partId\":\"1\"", json);
        Assert.DoesNotContain("null", json); // absent properties are omitted, not nulled
    }

    [Fact]
    public void Mailboxes_carry_roles_and_rights()
    {
        const string json =
            """
            {
              "id": "MB1", "name": "Inbox", "parentId": null, "role": "inbox", "sortOrder": 10,
              "totalEmails": 42, "unreadEmails": 3, "totalThreads": 40, "unreadThreads": 3,
              "myRights": {"mayReadItems": true, "mayAddItems": true, "mayRemoveItems": true,
                           "maySetSeen": true, "maySetKeywords": true, "mayCreateChild": true,
                           "mayRename": false, "mayDelete": false, "maySubmit": true},
              "isSubscribed": true
            }
            """;

        var mailbox = JsonSerializer.Deserialize<Mailbox>(json, JmapJson.Options)!;

        Assert.Equal(MailboxRoles.Inbox, mailbox.Role);
        Assert.Equal(42, mailbox.TotalEmails);
        Assert.False(mailbox.MyRights!.MayDelete);
    }
}
