using Grand.Infrastructure.Plugins;

namespace Grand.Business.Core.Interfaces.Authentication
{
    /// <summary>
    /// Represents method for the external authentication
    /// </summary>
    public partial interface IExternalAuthenticationProvider : IProvider
    {
        /// <summary>
        /// Gets a view component for displaying plugin in public store
        /// </summary>
        /// <param name="viewComponentName">View component name</param>
        Task<string> GetPublicViewComponentName();
    }
}
