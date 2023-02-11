using System.Security.Claims;

namespace Grand.Business.Core.Utilities.Authentication
{
    /// <summary>
    /// External auth param
    /// </summary>
    public class ExternalAuthParam
    {
        public ExternalAuthParam()
        {
            Claims = new List<Claim>();
        }

        /// <summary>
        /// Gets or sets the external authentication provider's system name
        /// </summary>
        public string ProviderSystemName { get; set; }

        /// <summary>
        /// Gets or sets user id 
        /// </summary>
        public string Identifier { get; set; }

        /// <summary>
        /// Gets or sets user name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets access token
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Gets or sets user email
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the additional user info as a list of a custom claims
        /// </summary>
        public IList<Claim> Claims { get; set; }
    }

}