using System.Text.Json;
using Jmap.Json;

namespace Jmap.Core.Tests;

public class SessionTests
{
    internal const string SessionJson =
        """
        {
          "capabilities": {
            "urn:ietf:params:jmap:core": {
              "maxSizeUpload": 50000000, "maxConcurrentUpload": 8, "maxSizeRequest": 10000000,
              "maxConcurrentRequests": 8, "maxCallsInRequest": 32, "maxObjectsInGet": 500,
              "maxObjectsInSet": 500, "collationAlgorithms": ["i;ascii-numeric", "i;ascii-casemap"]
            },
            "urn:ietf:params:jmap:mail": {}
          },
          "accounts": {
            "A13824": {
              "name": "john@example.com", "isPersonal": true, "isReadOnly": false,
              "accountCapabilities": { "urn:ietf:params:jmap:mail": { "maxMailboxesPerEmail": null, "maxMailboxDepth": 10 } }
            }
          },
          "primaryAccounts": { "urn:ietf:params:jmap:mail": "A13824" },
          "username": "john@example.com",
          "apiUrl": "https://jmap.example.com/api/",
          "downloadUrl": "https://jmap.example.com/download/{accountId}/{blobId}/{name}?accept={type}",
          "uploadUrl": "https://jmap.example.com/upload/{accountId}/",
          "eventSourceUrl": "https://jmap.example.com/eventsource/?types={types}&closeafter={closeafter}&ping={ping}",
          "state": "75128aab4b1b"
        }
        """;

    [Fact]
    public void Parses_the_rfc_example_session()
    {
        var session = JsonSerializer.Deserialize<JmapSession>(SessionJson, JmapJson.Options)!;

        Assert.Equal("john@example.com", session.Username);
        Assert.Equal("75128aab4b1b", session.State);
        Assert.Equal(new Uri("https://jmap.example.com/api/"), session.ApiUrl);

        var account = session.Accounts["A13824"];
        Assert.True(account.IsPersonal);
        Assert.Contains("urn:ietf:params:jmap:mail", account.AccountCapabilities.Keys);
    }

    [Fact]
    public void Exposes_a_typed_core_capability()
    {
        var session = JsonSerializer.Deserialize<JmapSession>(SessionJson, JmapJson.Options)!;

        Assert.True(session.TryGetCapability<CoreCapability>(JmapCapabilities.Core, out var core));
        Assert.Equal(50_000_000, core!.MaxSizeUpload);
        Assert.Equal(32, core.MaxCallsInRequest);
        Assert.Contains("i;ascii-casemap", core.CollationAlgorithms);
    }

    [Fact]
    public void Resolves_the_primary_account()
    {
        var session = JsonSerializer.Deserialize<JmapSession>(SessionJson, JmapJson.Options)!;

        Assert.Equal("A13824", session.PrimaryAccountId(JmapCapabilities.Mail));
        Assert.Equal("A13824", session.PrimaryAccountId()); // sole-account fallback for an unmapped urn
    }
}
