using System.Text.Json;
using Jmap.Json;

namespace Jmap.Mail.Tests;

public class MailMethodShapeTests
{
    [Fact]
    public void Email_query_carries_filter_sort_and_collapse_threads()
    {
        var call = Invocation.Create(MailMethods.EmailQuery, new EmailQueryArguments
        {
            AccountId = "A1",
            Filter = JmapFilter.And(
                new EmailFilterCondition { InMailbox = "MB1" },
                new EmailFilterCondition { HasKeyword = EmailKeywords.Flagged }),
            Sort = [new Comparator("receivedAt", IsAscending: false)],
            Limit = 50,
            CollapseThreads = true,
        }, "c0");

        var json = JsonSerializer.Serialize(call, JmapJson.Options);

        Assert.Contains("\"Email/query\"", json);
        Assert.Contains("\"filter\":{\"operator\":\"AND\",\"conditions\":[{\"inMailbox\":\"MB1\"},{\"hasKeyword\":\"$flagged\"}]}", json);
        Assert.Contains("\"collapseThreads\":true", json);
        Assert.Contains("\"sort\":[{\"property\":\"receivedAt\",\"isAscending\":false}]", json);
    }

    [Fact]
    public void Email_get_uses_the_rfc_casing_for_fetch_html()
    {
        var json = JsonSerializer.Serialize(new EmailGetArguments
        {
            AccountId = "A1",
            FetchHtmlBodyValues = true,
            MaxBodyValueBytes = 100_000,
        }, JmapJson.Options);

        Assert.Contains("\"fetchHTMLBodyValues\":true", json);
        Assert.Contains("\"maxBodyValueBytes\":100000", json);
    }

    [Fact]
    public void Submission_set_expresses_the_send_and_file_in_sent_pattern()
    {
        var arguments = new EmailSubmissionSetArguments
        {
            AccountId = "A1",
            Create = new Dictionary<string, EmailSubmission>
            {
                ["s1"] = new() { IdentityId = "I1", EmailId = "#draft1" },
            },
            OnSuccessUpdateEmail = new Dictionary<string, PatchObject>
            {
                ["#s1"] = PatchObject
                    .Set("mailboxIds/MB-sent", true)
                    .Without("mailboxIds/MB-drafts")
                    .With("keywords/$draft", null),
            },
        };

        var json = JsonSerializer.Serialize(arguments, JmapJson.Options);

        Assert.Contains("\"create\":{\"s1\":{\"identityId\":\"I1\",\"emailId\":\"#draft1\"}}", json);
        Assert.Contains(
            "\"onSuccessUpdateEmail\":{\"#s1\":{\"mailboxIds/MB-sent\":true,\"mailboxIds/MB-drafts\":null,\"keywords/$draft\":null}}",
            json);
    }

    [Fact]
    public void Changes_and_query_responses_parse()
    {
        const string changesJson =
            """
            {"accountId":"A1","oldState":"1","newState":"2","hasMoreChanges":false,
             "created":["M9"],"updated":[],"destroyed":["M2"],"updatedProperties":["totalEmails","unreadEmails"]}
            """;
        var changes = JsonSerializer.Deserialize<MailboxChangesResponse>(changesJson, JmapJson.Options)!;
        Assert.Equal(["M9"], changes.Created);
        Assert.Equal(["totalEmails", "unreadEmails"], changes.UpdatedProperties);

        const string queryJson =
            """{"accountId":"A1","queryState":"q1","canCalculateChanges":true,"position":0,"ids":["M1","M2"],"total":2}""";
        var query = JsonSerializer.Deserialize<QueryResponse>(queryJson, JmapJson.Options)!;
        Assert.Equal(2, query.Ids.Count);
        Assert.True(query.CanCalculateChanges);
    }

    [Fact]
    public void Email_set_response_reports_per_object_errors()
    {
        const string json =
            """
            {"accountId":"A1","oldState":"1","newState":"2",
             "created":{"d1":{"id":"M77","blobId":"B77","threadId":"T77","size":100}},
             "notCreated":{"d2":{"type":"invalidProperties","properties":["to"]}}}
            """;

        var response = JsonSerializer.Deserialize<SetResponse<Email>>(json, JmapJson.Options)!;

        Assert.Equal("M77", response.Created!["d1"].Id);
        Assert.Equal("invalidProperties", response.NotCreated!["d2"].Type);
        Assert.Equal(["to"], response.NotCreated["d2"].Properties);
    }
}
