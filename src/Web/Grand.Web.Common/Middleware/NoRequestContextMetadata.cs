namespace Grand.Web.Common.Middleware;

/// <summary>
///     Endpoint metadata: ContextMiddleware sets neither store nor work context for the endpoint. For anonymous endpoints
///     called server to server, which need no customer: a cookieless request would otherwise insert a guest
/// </summary>
public sealed class NoRequestContextMetadata
{
    public static readonly NoRequestContextMetadata Instance = new();

    private NoRequestContextMetadata()
    {
    }
}
