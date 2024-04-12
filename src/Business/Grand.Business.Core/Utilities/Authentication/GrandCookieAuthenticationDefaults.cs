using Microsoft.AspNetCore.Http;

namespace Grand.Business.Core.Utilities.Authentication;

public static class GrandCookieAuthenticationDefaults
{
    /// <summary>
    ///     The default value used for authentication scheme
    /// </summary>
    public const string AuthenticationScheme = "Authentication";

    /// <summary>
    ///     The default value used for external authentication scheme
    /// </summary>
    public const string ExternalAuthenticationScheme = "ExternalAuthentication";

    /// <summary>
    ///     The default value for the login path
    /// </summary>
    public static readonly PathString LoginPath = new("/login");

    /// <summary>
    ///     The default value for the access denied path
    /// </summary>
    public static readonly PathString AccessDeniedPath = new("/access-denied");
}