using System.Text.Json;
using Jmap.Json;

namespace Jmap.Core.Tests;

public class EventSourceParserTests
{
    [Fact]
    public async Task Yields_state_events_and_ignores_pings_and_other_events()
    {
        const string stream =
            ": comment to ignore\n" +
            "event: state\n" +
            "data: {\"changed\":{\"A1\":{\"Email\":\"s1\"}}}\n" +
            "\n" +
            "event: ping\n" +
            "data: {\"interval\":60}\n" +
            "\n" +
            "event: state\n" +
            "data: {\"changed\":{\"A1\":{\"Email\":\"s2\",\"Mailbox\":\"m5\"}}}\n" +
            "\n";

        var events = new List<StateChange>();
        await foreach (var data in EventSourceParser.ReadEventsAsync(new StringReader(stream), "state"))
        {
            events.Add(JsonSerializer.Deserialize<StateChange>(data, JmapJson.Options)!);
        }

        Assert.Equal(2, events.Count);
        Assert.Equal("s1", events[0].Changed["A1"]["Email"]);
        Assert.Equal("m5", events[1].Changed["A1"]["Mailbox"]);
    }

    [Fact]
    public async Task Multi_line_data_joins_with_newlines()
    {
        const string stream = "event: state\ndata: {\"changed\":\ndata: {}}\n\n";

        var events = new List<string>();
        await foreach (var data in EventSourceParser.ReadEventsAsync(new StringReader(stream), "state"))
        {
            events.Add(data);
        }

        Assert.Equal("{\"changed\":\n{}}", Assert.Single(events));
    }
}
