# Jmap.Net

A dependency-light .NET client library for **JMAP**, the JSON Meta Application Protocol:

- **`Jmap.Core`** — RFC 8620: the session resource, the request/response envelope with
  method batching and back-references (`#argument`), the standard `/get` `/set` `/changes`
  `/query` `/queryChanges` `/copy` method shapes, filters and patch objects, blob
  upload/download, push over EventSource, and `PushSubscription` objects for Web Push.
  Plus the core extensions: JMAP over WebSocket (RFC 8887), blob management —
  `Blob/upload`, `Blob/get`, `Blob/lookup` (RFC 9404), and quotas (RFC 9425).
  No dependencies beyond the BCL.
- **`Jmap.Mail`** — RFC 8621: the full mail object model (`Mailbox`, `Email`, `Thread`,
  `SearchSnippet`, `Identity`, `EmailSubmission`, `VacationResponse`), every method name and
  the mail-specific method arguments (body-fetch controls, `collapseThreads`,
  `onSuccessUpdateEmail`, `Email/import`, `Email/parse`, …). Plus the mail extensions:
  MDN read receipts — `MDN/send`, `MDN/parse` (RFC 9007) and S/MIME verification status
  (RFC 9219).

Targets `net8.0` and `net10.0`. Licensed MIT.

## Quick start

```csharp
using Jmap;
using Jmap.Mail;

using var client = new JmapClient(new JmapClientOptions
{
    SessionUrl = new Uri("https://api.fastmail.com/jmap/session"),
    BearerToken = token,
});

var session = await client.ConnectAsync();
var accountId = session.PrimaryAccountId(JmapCapabilities.Mail);

// Newest 10 inbox threads — ids and headers in a single round trip via a back-reference.
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
    Console.WriteLine($"{email.ReceivedAt:g}  {email.Subject}");
}
```

Sending mail is a draft `Email/set` create plus an `EmailSubmission/set` — with
`OnSuccessUpdateEmail` moving the message out of Drafts atomically. Push is
`client.GetEventsAsync(...)`, an `IAsyncEnumerable<StateChange>` over the session's
EventSource endpoint. Blobs are `UploadBlobAsync`/`DownloadBlobAsync`.

When the server advertises `urn:ietf:params:jmap:websocket`, the same requests can run
over one socket instead (RFC 8887), with push on the same connection:

```csharp
await using var socket = await client.ConnectWebSocketAsync();
await socket.EnablePushAsync(["Email", "Mailbox"]);
var response = await socket.SendAsync(request);           // correlated by id, so calls
await foreach (var change in socket.GetEventsAsync()) { } // and events interleave freely
```

`samples/JmapCli` is a runnable smoke test:

```bash
JMAP_SESSION_URL=https://api.fastmail.com/jmap/session JMAP_TOKEN=... \
    dotnet run --project samples/JmapCli -- mailboxes
```

## Design notes

- **Requests are explicit.** You build the method batch yourself (`JmapRequest` /
  `Invocation`) rather than going through a per-method facade — JMAP's power is batching
  with back-references, and a facade that hides the batch hides the protocol's main win.
- **Typed where the RFC is typed, JSON where it's open.** Method arguments/responses,
  objects and filters are records; session capability objects and server-set `updated`
  values stay as `JsonElement` for forward compatibility.
- **Errors are layered like the RFC:** `JmapRequestException` (HTTP problem details),
  `JmapMethodException` (method-level error), and `SetError` inside `/set` responses.
- Filters serialise their runtime type; `FilterOperator` composes them
  (`JmapFilter.And(...)`). Patches are `PatchObject` path/value maps
  (`PatchObject.Set("keywords/$seen", true)`).

## Status / roadmap

- [x] RFC 8620 core: session, envelope, back-references, standard methods, filters,
  patches, blobs, EventSource push
- [x] RFC 8621 mail: full object model, all method names + mail-specific shapes
- [x] PushSubscription objects (RFC 8620 §7.2 — server-initiated Web Push)
- [x] WebSocket transport (RFC 8887), including per-connection push
- [x] MDN read receipts (RFC 9007: `MDN/send`, `MDN/parse`, `$mdnsent`)
- [x] S/MIME verification status (RFC 9219: `smimeStatus` &co. + query filters)
- [x] Blob management (RFC 9404: `Blob/upload`, `Blob/get`, `Blob/lookup`)
- [x] Quotas (RFC 9425)
- [ ] Sieve script management (RFC 9661) — server-side filter editing, beyond a mail
  client's core needs
- [ ] Higher-level sync helper (state tracking + `/changes` loops) once a consumer proves
  the shape
- [ ] Integration test suite against a containerised Stalwart server

## Building

```bash
dotnet build
dotnet test
dotnet pack -c Release -o artifacts   # NuGet packages
```
