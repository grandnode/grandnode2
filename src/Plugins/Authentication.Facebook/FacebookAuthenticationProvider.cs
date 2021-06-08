using Grand.Business.Authentication.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Authentication.Facebook
{
    public class FacebookAuthenticationProvider : IExternalAuthenticationProvider
    {
        private readonly FacebookExternalAuthSettings _facebookExternalAuthSettings;

        public FacebookAuthenticationProvider(FacebookExternalAuthSettings facebookExternalAuthSettings)
        {
            _facebookExternalAuthSettings = facebookExternalAuthSettings;
        }
        public string SystemName => FacebookAuthenticationDefaults.ProviderSystemName;

        public string FriendlyName => "Facebook authentication";

        public int Priority => _facebookExternalAuthSettings.DisplayOrder;

        public string ConfigurationUrl => FacebookAuthenticationDefaults.ConfigurationUrl;

        public IList<string> LimitedToStores => new List<string>();

        public IList<string> LimitedToGroups => new List<string>();

        /// <summary>
        /// Gets a view component for displaying plugin in public store
        /// </summary>
        /// <param name="viewComponentName">View component name</param>
        public async Task<string> GetPublicViewComponentName()
        {
            return await Task.FromResult("FacebookAuthentication");
        }
    }
}
