namespace Jmap;

/// <summary>The capability URNs a client lists in a request's <c>using</c> array.</summary>
public static class JmapCapabilities
{
    public const string Core = "urn:ietf:params:jmap:core";                          // RFC 8620
    public const string Mail = "urn:ietf:params:jmap:mail";                          // RFC 8621
    public const string Submission = "urn:ietf:params:jmap:submission";              // RFC 8621
    public const string VacationResponse = "urn:ietf:params:jmap:vacationresponse";  // RFC 8621
    public const string WebSocket = "urn:ietf:params:jmap:websocket";                // RFC 8887
}
