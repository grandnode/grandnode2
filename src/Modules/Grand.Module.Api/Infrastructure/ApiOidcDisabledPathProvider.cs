using Grand.Business.Core.Interfaces.Authentication;
using Grand.Infrastructure.Configuration;
using Grand.Module.Api.Constants;

namespace Grand.Module.Api.Infrastructure;

/// <summary>
///     While BackendAPI.Enabled is false the Web API's paths are withdrawn from OIDC protection, so an access token
///     there is refused by the Bearer scheme before it is mapped to a customer or pins the store
/// </summary>
public class ApiOidcDisabledPathProvider(BackendAPIConfig config) : IOidcDisabledPathProvider
{
    public IEnumerable<string> GetDisabledPaths() =>
        config.Enabled ? [] : ["/" + Configurations.RestRoutePrefix];
}
