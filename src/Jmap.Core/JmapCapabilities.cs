namespace Jmap;

/// <summary>The capability URNs a client lists in a request's <c>using</c> array.</summary>
public static class JmapCapabilities
{
    public const string Core = "urn:ietf:params:jmap:core";                          // RFC 8620
    public const string Mail = "urn:ietf:params:jmap:mail";                          // RFC 8621
    public const string Submission = "urn:ietf:params:jmap:submission";              // RFC 8621
    public const string VacationResponse = "urn:ietf:params:jmap:vacationresponse";  // RFC 8621
    public const string WebSocket = "urn:ietf:params:jmap:websocket";                // RFC 8887
    public const string Mdn = "urn:ietf:params:jmap:mdn";                            // RFC 9007
    public const string SmimeVerify = "urn:ietf:params:jmap:smimeverify";            // RFC 9219
    public const string Blob = "urn:ietf:params:jmap:blob";                          // RFC 9404
    public const string Quota = "urn:ietf:params:jmap:quota";                        // RFC 9425
}
