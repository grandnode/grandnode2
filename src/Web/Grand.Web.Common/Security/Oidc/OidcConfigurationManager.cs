using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Grand.Web.Common.Security.Oidc;

/// <summary>
///     The JwtBearer configuration manager, remembering whether the last attempt to obtain the authorization server's
///     metadata and signing keys failed. The token handler swallows that failure and then rejects the token as
///     issued by another issuer or badly signed; the Oidc events read <see cref="Unavailable" /> to answer 503
///     instead of telling the client its valid token is invalid.
/// </summary>
public class OidcConfigurationManager : ConfigurationManager<OpenIdConnectConfiguration>
{
    private volatile bool _unavailable;

    public OidcConfigurationManager(string metadataAddress, IDocumentRetriever documentRetriever)
        : base(metadataAddress, new OpenIdConnectConfigurationRetriever(), documentRetriever)
    {
    }

    /// <summary>True when the last attempt obtained no configuration (none cached and the retrieval failed)</summary>
    public bool Unavailable => _unavailable;

    public override async Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel)
    {
        try
        {
            var configuration = await base.GetConfigurationAsync(cancel);
            _unavailable = false;
            return configuration;
        }
        catch (Exception) when (!cancel.IsCancellationRequested)
        {
            _unavailable = true;
            throw;
        }
    }
}
