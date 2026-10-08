namespace Grand.Infrastructure.Configuration;

/// <summary>
///     An API area protected by access tokens of the identity provider
/// </summary>
public class OidcProtectedResource
{
    /// <summary>Unique name, also published as resource_name</summary>
    public string Name { get; set; }

    /// <summary>Path prefix, matched case-insensitively on a segment boundary ("/api" covers "/api/x", not "/apix")</summary>
    public string PathPrefix { get; set; }

    /// <summary>Canonical resource URI (RFC 8707); empty = the request's base URL + PathPrefix</summary>
    public string Resource { get; set; }

    /// <summary>Additional accepted "aud" values, for providers that do not put the resource URI there</summary>
    public List<string> Audiences { get; set; } = [];

    /// <summary>When not empty, "azp" (or "client_id") must be one of these</summary>
    public List<string> AuthorizedParties { get; set; } = [];

    public List<string> ScopesSupported { get; set; } = [];
    public List<string> RequiredScopes { get; set; } = [];

    /// <summary>The customer needs the admin panel access permission</summary>
    public bool RequireAdminPanelAccess { get; set; }

    public bool DenyVendors { get; set; }
    public bool DenyStoreManagers { get; set; }
}
