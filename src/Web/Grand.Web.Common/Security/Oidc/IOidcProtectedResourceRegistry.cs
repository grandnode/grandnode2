using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Common.Security.Oidc;

public interface IOidcProtectedResourceRegistry
{
    /// <summary>Valid resources from configuration and plugins; empty when the resource server is disabled</summary>
    IReadOnlyList<OidcProtectedResource> Resources { get; }

    /// <summary>Configuration problems found while merging (reported at startup)</summary>
    IReadOnlyList<string> Errors { get; }

    /// <summary>Configured resources left out because the module serving their path is switched off</summary>
    IReadOnlyList<OidcProtectedResource> Withdrawn { get; }

    /// <summary>The resource covering the path, or null</summary>
    OidcProtectedResource Match(PathString path);
}
