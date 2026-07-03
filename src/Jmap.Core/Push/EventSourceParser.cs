using System.Runtime.CompilerServices;
using System.Text;

namespace Jmap;

/// <summary>Minimal text/event-stream reader: yields the data of events matching a given
/// event name, ignoring comments, pings and other event types.</summary>
internal static class EventSourceParser
{
    public static async IAsyncEnumerable<string> ReadEventsAsync(
        TextReader reader, string eventName, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var currentEvent = "message"; // the SSE default when no "event:" field is sent
        var data = new StringBuilder();

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.Length == 0)
            {
                // Blank line dispatches the accumulated event.
                if (data.Length > 0 && currentEvent == eventName)
                {
                    yield return data.ToString();
                }

                currentEvent = "message";
                data.Clear();
                continue;
            }

            if (line.StartsWith(':'))
            {
                continue; // comment / keep-alive ping
            }

            var (field, value) = SplitField(line);
            switch (field)
            {
                case "event":
                    currentEvent = value;
                    break;
                case "data":
                    if (data.Length > 0)
                    {
                        data.Append('\n');
                    }

                    data.Append(value);
                    break;
            }
        }
    }

    private static (string Field, string Value) SplitField(string line)
    {
        var colon = line.IndexOf(':');
        if (colon < 0)
        {
            return (line, string.Empty);
        }

        var value = line[(colon + 1)..];
        return (line[..colon], value.StartsWith(' ') ? value[1..] : value);
    }
}
