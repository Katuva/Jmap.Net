using System.Text.Json;
using Jmap.Json;

namespace Jmap.Core.Tests;

public class PushSubscriptionTests
{
    [Fact]
    public void Set_create_serialises_without_account_or_state_arguments()
    {
        var arguments = new PushSubscriptionSetArguments
        {
            Create = new Dictionary<string, PushSubscription>
            {
                ["sub1"] = new()
                {
                    DeviceClientId = "a889-ffea-910",
                    Url = "https://example.com/push/?device=X8980fc&client=12c6d086",
                    Keys = new PushSubscriptionKeys("dGhpcyBpcyBh", "c2VjcmV0"),
                    Types = ["Email", "Mailbox"],
                },
            },
        };

        var json = JsonSerializer.Serialize(arguments, JmapJson.Options);

        Assert.DoesNotContain("accountId", json);
        Assert.DoesNotContain("ifInState", json);
        Assert.Contains("\"deviceClientId\":\"a889-ffea-910\"", json);
        Assert.Contains("\"keys\":{\"p256dh\":\"dGhpcyBpcyBh\",\"auth\":\"c2VjcmV0\"}", json);
        Assert.Contains("\"types\":[\"Email\",\"Mailbox\"]", json);
    }

    [Fact]
    public void Verification_update_and_expires_use_the_utc_date_form()
    {
        var arguments = new PushSubscriptionSetArguments
        {
            Update = new Dictionary<string, PatchObject>
            {
                ["P43dcfa4-1dd4"] = PatchObject.Set("verificationCode", "b1e0f6c9"),
            },
        };
        Assert.Contains(
            "\"update\":{\"P43dcfa4-1dd4\":{\"verificationCode\":\"b1e0f6c9\"}}",
            JsonSerializer.Serialize(arguments, JmapJson.Options));

        var subscription = new PushSubscription
        {
            DeviceClientId = "d",
            Url = "https://example.com/push",
            Expires = new DateTimeOffset(2026, 7, 4, 15, 0, 0, TimeSpan.FromHours(2)),
        };
        Assert.Contains("\"expires\":\"2026-07-04T13:00:00Z\"", JsonSerializer.Serialize(subscription, JmapJson.Options));
    }

    [Fact]
    public void Get_response_and_push_verification_parse()
    {
        const string getJson =
            """
            {"list":[{"id":"P43dcfa4-1dd4","deviceClientId":"a889-ffea-910","verificationCode":"b1e0f6c9","expires":"2018-07-31T00:13:21Z","types":["Todo"]}],
             "notFound":[]}
            """;
        var response = JsonSerializer.Deserialize<PushSubscriptionGetResponse>(getJson, JmapJson.Options)!;
        Assert.Equal("P43dcfa4-1dd4", response.List[0].Id);
        Assert.Equal(["Todo"], response.List[0].Types!);

        const string verificationJson =
            """{"@type":"PushVerification","pushSubscriptionId":"P43dcfa4-1dd4","verificationCode":"b1e0f6c9"}""";
        var verification = JsonSerializer.Deserialize<PushVerification>(verificationJson, JmapJson.Options)!;
        Assert.Equal("P43dcfa4-1dd4", verification.PushSubscriptionId);
        Assert.Equal("b1e0f6c9", verification.VerificationCode);
    }
}
