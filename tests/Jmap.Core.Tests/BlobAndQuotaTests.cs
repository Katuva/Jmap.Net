using System.Text.Json;
using Jmap.Json;

namespace Jmap.Core.Tests;

public class BlobAndQuotaTests
{
    [Fact]
    public void Blob_upload_serialises_the_colon_named_data_sources()
    {
        var arguments = new BlobUploadArguments
        {
            AccountId = "A1",
            Create = new Dictionary<string, BlobUploadObject>
            {
                ["b4"] = new()
                {
                    Data =
                    [
                        BlobDataSource.Text("The quick brown fox"),
                        BlobDataSource.Base64("!"u8),
                        BlobDataSource.FromBlob("G123", offset: 0, length: 5),
                    ],
                    Type = "text/plain",
                },
            },
        };

        var json = JsonSerializer.Serialize(arguments, JmapJson.Options);

        Assert.Contains("\"data\":[{\"data:asText\":\"The quick brown fox\"},{\"data:asBase64\":\"IQ==\"},{\"blobId\":\"G123\",\"offset\":0,\"length\":5}]", json);
        Assert.Contains("\"type\":\"text/plain\"", json);
    }

    [Fact]
    public void Blob_get_returns_data_and_algorithm_named_digests()
    {
        const string json =
            """
            {"accountId":"A1",
             "list":[{"id":"G123","data:asText":"The quick brown fox","digest:sha":"XV4BgGz0VYd+kv9NrGjdCBIcTFE=","size":19},
                     {"id":"G456","data:asText":null,"data:asBase64":"AgQ=","isEncodingProblem":true,"size":2}],
             "notFound":["not-a-blob"]}
            """;

        var response = JsonSerializer.Deserialize<BlobGetResponse>(json, JmapJson.Options)!;

        Assert.Equal("The quick brown fox", response.List[0].DataAsText);
        Assert.Equal("XV4BgGz0VYd+kv9NrGjdCBIcTFE=", response.List[0].Digest("sha"));
        Assert.Null(response.List[0].Digest("sha-256"));
        Assert.True(response.List[1].IsEncodingProblem);
        Assert.Equal("AgQ=", response.List[1].DataAsBase64);
        Assert.Equal(["not-a-blob"], response.NotFound);
    }

    [Fact]
    public void Blob_lookup_maps_type_names_to_referencing_ids()
    {
        const string json =
            """
            {"accountId":"A1",
             "list":[{"id":"G123","matchedIds":{"Email":["E17","E18"],"Mailbox":[],"Thread":["T1"]}}],
             "notFound":[]}
            """;

        var response = JsonSerializer.Deserialize<BlobLookupResponse>(json, JmapJson.Options)!;

        Assert.Equal(["E17", "E18"], response.List[0].MatchedIds["Email"]);
        Assert.Empty(response.List[0].MatchedIds["Mailbox"]);
    }

    [Fact]
    public void Quota_objects_and_filters_use_the_rfc_shapes()
    {
        const string json =
            """
            {"accountId":"A1","state":"78540","list":[
              {"id":"2a","resourceType":"count","used":1056,"warnLimit":1600,"softLimit":1800,
               "hardLimit":2000,"scope":"account","name":"bob@example.com",
               "description":"Personal account usage","types":["Mail","Calendar"]}],
             "notFound":[]}
            """;

        var response = JsonSerializer.Deserialize<GetResponse<Quota>>(json, JmapJson.Options)!;
        var quota = response.List[0];
        Assert.Equal(QuotaResourceTypes.Count, quota.ResourceType);
        Assert.Equal(QuotaScopes.Account, quota.Scope);
        Assert.Equal(2000, quota.HardLimit);
        Assert.Equal(["Mail", "Calendar"], quota.Types);

        var filter = JsonSerializer.Serialize<JmapFilter>(
            new QuotaFilterCondition { Scope = QuotaScopes.Account, Type = "Mail" }, JmapJson.Options);
        Assert.Equal("""{"scope":"account","type":"Mail"}""", filter);

        const string changesJson =
            """{"accountId":"A1","oldState":"1","newState":"2","hasMoreChanges":false,"updated":["2a"],"updatedProperties":["used"]}""";
        var changes = JsonSerializer.Deserialize<QuotaChangesResponse>(changesJson, JmapJson.Options)!;
        Assert.Equal(["used"], changes.UpdatedProperties);
    }

    [Fact]
    public void Blob_copy_round_trips()
    {
        var json = JsonSerializer.Serialize(new BlobCopyArguments
        {
            FromAccountId = "A1",
            AccountId = "A2",
            BlobIds = ["G123"],
        }, JmapJson.Options);
        Assert.Equal("""{"fromAccountId":"A1","accountId":"A2","blobIds":["G123"]}""", json);

        const string responseJson =
            """{"fromAccountId":"A1","accountId":"A2","copied":{"G123":"G777"},"notCopied":null}""";
        var response = JsonSerializer.Deserialize<BlobCopyResponse>(responseJson, JmapJson.Options)!;
        Assert.Equal("G777", response.Copied!["G123"]);
    }
}
