using Grand.Business.Core.Interfaces.Authentication;
using Grand.Domain.Customers;

namespace Grand.Business.Core.Extensions
{
    /// <summary>
    /// Extensions of external authentication method 
    /// </summary>
    public static class ExternalAuthenticationProviderExtensions
    {
        /// <summary>
        /// Check whether external authentication method is active
        /// </summary>
        /// <param name="method">External authentication method</param>
        /// <param name="settings">External authentication settings</param>
        /// <returns>True if method is active; otherwise false</returns>
        public static bool IsMethodActive(this IExternalAuthenticationProvider method, ExternalAuthenticationSettings settings)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (settings.ActiveAuthenticationMethodSystemNames == null)
                return false;

            foreach (string activeMethodSystemName in settings.ActiveAuthenticationMethodSystemNames)
                if (method.SystemName.Equals(activeMethodSystemName, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }
    }
}
