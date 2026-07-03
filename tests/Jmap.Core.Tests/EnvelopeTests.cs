using System.Text.Json;
using Jmap.Json;

namespace Jmap.Core.Tests;

public class EnvelopeTests
{
    [Fact]
    public void Request_serialises_invocations_as_three_element_arrays()
    {
        var request = new JmapRequest
        {
            Using = [JmapCapabilities.Core, JmapCapabilities.Mail],
            MethodCalls = [Invocation.Create("Mailbox/get", new { accountId = "A1" }, "c0")],
        };

        var json = JsonSerializer.Serialize(request, JmapJson.Options);

        Assert.Equal(
            """{"using":["urn:ietf:params:jmap:core","urn:ietf:params:jmap:mail"],"methodCalls":[["Mailbox/get",{"accountId":"A1"},"c0"]]}""",
            json);
    }

    [Fact]
    public void Back_references_replace_the_literal_argument_with_a_hash_key()
    {
        var call = Invocation
            .Create("Email/get", new { accountId = "A1", ids = Array.Empty<string>() }, "c1")
            .WithBackReference("ids", new ResultReference("c0", "Email/query", "/ids"));

        var json = JsonSerializer.Serialize(call, JmapJson.Options);

        Assert.Contains("\"#ids\":{\"resultOf\":\"c0\",\"name\":\"Email/query\",\"path\":\"/ids\"}", json);
        Assert.DoesNotContain("\"ids\"", json.Replace("\"#ids\"", ""));
    }

    [Fact]
    public void Response_parses_and_resolves_typed_results()
    {
        const string json =
            """
            {
              "methodResponses": [
                ["Mailbox/get", {"accountId": "A1", "state": "s1", "list": [], "notFound": []}, "c0"],
                ["error", {"type": "unknownMethod"}, "c1"]
              ],
              "sessionState": "s-42"
            }
            """;

        var response = JsonSerializer.Deserialize<JmapResponse>(json, JmapJson.Options)!;

        Assert.Equal("s-42", response.SessionState);
        var mailboxes = response.Require<GetResponse<JsonElement>>("c0");
        Assert.Equal("s1", mailboxes.State);

        var failure = Assert.Throws<JmapMethodException>(() => response.Require<GetResponse<JsonElement>>("c1"));
        Assert.Equal(JmapErrorTypes.UnknownMethod, failure.Error.Type);
    }

    [Fact]
    public void Created_ids_round_trip()
    {
        var request = new JmapRequest
        {
            Using = [JmapCapabilities.Core],
            MethodCalls = [Invocation.Create("Core/echo", new { hello = true }, "c0")],
            CreatedIds = new Dictionary<string, string> { ["draft1"] = "M123" },
        };

        var json = JsonSerializer.Serialize(request, JmapJson.Options);

        Assert.Contains("""createdIds":{"draft1":"M123"}""", json);
    }
}
