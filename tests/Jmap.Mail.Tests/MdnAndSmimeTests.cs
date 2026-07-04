using System.Text.Json;
using Jmap.Json;

namespace Jmap.Mail.Tests;

public class MdnAndSmimeTests
{
    [Fact]
    public void Mdn_send_expresses_the_read_receipt_pattern()
    {
        var arguments = new MdnSendArguments
        {
            AccountId = "A1",
            IdentityId = "I1",
            Send = new Dictionary<string, Mdn>
            {
                ["k1546"] = new()
                {
                    ForEmailId = "Md45b47b4877521042cec0938",
                    Subject = "Read receipt for: World domination",
                    TextBody = "This receipt shows that the email has been displayed on your recipient's computer.",
                    ReportingUA = "joes-pc.cs.example.com; Foomail 97.1",
                    Disposition = new MdnDisposition(
                        MdnActionModes.ManualAction, MdnSendingModes.SentManually, MdnDispositionTypes.Displayed),
                },
            },
            OnSuccessUpdateEmail = new Dictionary<string, PatchObject>
            {
                ["#k1546"] = PatchObject.Set("keywords/" + EmailKeywords.MdnSent, true),
            },
        };

        var json = JsonSerializer.Serialize(arguments, JmapJson.Options);

        Assert.Contains("\"identityId\":\"I1\"", json);
        Assert.Contains("\"reportingUA\":\"joes-pc.cs.example.com; Foomail 97.1\"", json);
        Assert.Contains(
            "\"disposition\":{\"actionMode\":\"manual-action\",\"sendingMode\":\"mdn-sent-manually\",\"type\":\"displayed\"}",
            json);
        Assert.Contains("\"onSuccessUpdateEmail\":{\"#k1546\":{\"keywords/$mdnsent\":true}}", json);
    }

    [Fact]
    public void Mdn_parse_response_round_trips_the_server_set_fields()
    {
        const string json =
            """
            {"accountId":"A1",
             "parsed":{"Bd23":{
               "forEmailId":"Md888","subject":"Read receipt for: World domination",
               "originalMessageId":"<199509192301.23456@example.org>",
               "originalRecipient":"rfc822; joe@example.com",
               "finalRecipient":"rfc822; joe@example.com",
               "includeOriginalMessage":false,
               "disposition":{"actionMode":"manual-action","sendingMode":"mdn-sent-manually","type":"displayed"},
               "error":["Problem 1"],
               "extensionFields":{"X-EXTENSION-EXAMPLE":"example.com"}}},
             "notParsable":["Bd99"],
             "notFound":null}
            """;

        var response = JsonSerializer.Deserialize<MdnParseResponse>(json, JmapJson.Options)!;
        var mdn = response.Parsed!["Bd23"];

        Assert.Equal("<199509192301.23456@example.org>", mdn.OriginalMessageId);
        Assert.Equal(MdnDispositionTypes.Displayed, mdn.Disposition!.Type);
        Assert.Equal(["Problem 1"], mdn.Error!);
        Assert.Equal("example.com", mdn.ExtensionFields!["X-EXTENSION-EXAMPLE"]);
        Assert.Equal(["Bd99"], response.NotParsable!);
    }

    [Fact]
    public void Smime_properties_parse_and_filter_conditions_serialise()
    {
        const string json =
            """
            {"id":"M123","smimeStatus":"signed/verified","smimeStatusAtDelivery":"signed/verified",
             "smimeErrors":null,"smimeVerifiedAt":"2022-01-08T09:00:00Z"}
            """;
        var email = JsonSerializer.Deserialize<Email>(json, JmapJson.Options)!;
        Assert.Equal(SmimeStatuses.SignedVerified, email.SmimeStatus);
        Assert.Equal(new DateTimeOffset(2022, 1, 8, 9, 0, 0, TimeSpan.Zero), email.SmimeVerifiedAt);

        var filter = JsonSerializer.Serialize<JmapFilter>(
            new EmailFilterCondition { HasVerifiedSmime = true }, JmapJson.Options);
        Assert.Equal("""{"hasVerifiedSmime":true}""", filter);
    }
}
