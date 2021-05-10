using Grand.Infrastructure.Plugins;
using System.Threading.Tasks;

namespace Grand.Business.Authentication.Interfaces
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
