using Grand.Business.Core.Interfaces.Authentication;
using Grand.Domain.Customers;

namespace Grand.Business.Core.Extensions;

/// <summary>
///     Extensions of external authentication method
/// </summary>
public static class ExternalAuthenticationProviderExtensions
{
    /// <summary>
    ///     Check whether external authentication method is active
    /// </summary>
    /// <param name="method">External authentication method</param>
    /// <param name="settings">External authentication settings</param>
    /// <returns>True if method is active; otherwise false</returns>
    public static bool IsMethodActive(this IExternalAuthenticationProvider method,
        ExternalAuthenticationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(settings);

        return settings.ActiveAuthenticationMethodSystemNames != null &&
               settings.ActiveAuthenticationMethodSystemNames.Any(activeMethodSystemName =>
                   method.SystemName.Equals(activeMethodSystemName, StringComparison.OrdinalIgnoreCase));
    }
}