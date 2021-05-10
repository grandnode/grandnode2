using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;

namespace Grand.Business.Authentication.Interfaces
{
    /// <summary>
    /// Interface to add authentication 
    /// </summary>
    public interface IAuthenticationBuilder
    {
        /// <summary>
        /// Add Authentication
        /// </summary>
        /// <param name="builder">Add Authentication builder</param>
        void AddAuthentication(AuthenticationBuilder builder, IConfiguration configuration);

        /// <summary>
        /// Gets priority
        /// </summary>
        int Priority { get; }
    }
}
