using Jmap;
using Jmap.Mail;

// A tiny smoke-test client. Usage:
//   JMAP_SESSION_URL=https://api.fastmail.com/jmap/session JMAP_TOKEN=... dotnet run -- [session|mailboxes|inbox|watch]

var sessionUrl = Environment.GetEnvironmentVariable("JMAP_SESSION_URL");
var token = Environment.GetEnvironmentVariable("JMAP_TOKEN");
if (sessionUrl is null || token is null)
{
    Console.Error.WriteLine("Set JMAP_SESSION_URL and JMAP_TOKEN.");
    return 1;
}

using var client = new JmapClient(new JmapClientOptions { SessionUrl = new Uri(sessionUrl), BearerToken = token });
var session = await client.ConnectAsync();
var accountId = session.PrimaryAccountId(JmapCapabilities.Mail);

switch (args is [var verb] ? verb : "session")
{
    case "session":
        Console.WriteLine($"User      {session.Username}");
        Console.WriteLine($"API       {session.ApiUrl}");
        Console.WriteLine($"Account   {accountId}");
        Console.WriteLine($"Caps      {string.Join(", ", session.Capabilities.Keys)}");
        break;

    case "mailboxes":
    {
        var mailboxes = await client.CallAsync<GetResponse<Mailbox>>(
            MailMethods.MailboxGet, new GetArguments { AccountId = accountId }, [JmapCapabilities.Core, JmapCapabilities.Mail]);
        foreach (var mailbox in mailboxes.List.OrderBy(m => m.SortOrder))
        {
            Console.WriteLine($"{mailbox.Id,-12} {mailbox.Role ?? "-",-9} {mailbox.Name} ({mailbox.UnreadEmails}/{mailbox.TotalEmails})");
        }

        break;
    }

    case "inbox":
    {
        // One round trip: query newest ids, then fetch their headers via a back-reference.
        var response = await client.SendAsync(new JmapRequest
        {
            Using = [JmapCapabilities.Core, JmapCapabilities.Mail],
            MethodCalls =
            [
                Invocation.Create(MailMethods.EmailQuery, new EmailQueryArguments
                {
                    AccountId = accountId,
                    Sort = [new Comparator("receivedAt", IsAscending: false)],
                    Limit = 10,
                    CollapseThreads = true,
                }, "c0"),
                Invocation.Create(MailMethods.EmailGet, new GetArguments
                {
                    AccountId = accountId,
                    Properties = ["id", "subject", "from", "receivedAt", "preview"],
                }, "c1").WithBackReference("ids", new ResultReference("c0", MailMethods.EmailQuery, "/ids")),
            ],
        });

        foreach (var email in response.Require<GetResponse<Email>>("c1").List)
        {
            var from = email.From is [{ } first, ..] ? first.Name ?? first.Email : "?";
            Console.WriteLine($"{email.ReceivedAt:yyyy-MM-dd HH:mm}  {from,-24}  {email.Subject}");
        }

        break;
    }

    case "watch":
        Console.WriteLine("Waiting for push events (Ctrl-C to stop)…");
        await foreach (var change in client.GetEventsAsync(["Email", "Mailbox"]))
        {
            foreach (var (account, types) in change.Changed)
            {
                Console.WriteLine($"{DateTimeOffset.Now:HH:mm:ss} {account}: {string.Join(", ", types.Select(t => $"{t.Key}={t.Value}"))}");
            }
        }

        break;

    default:
        Console.Error.WriteLine("Unknown command. Use: session | mailboxes | inbox | watch");
        return 1;
}

return 0;
