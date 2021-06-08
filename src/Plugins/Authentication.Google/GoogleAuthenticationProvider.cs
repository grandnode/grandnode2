using Grand.Business.Authentication.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Authentication.Google
{
    public class GoogleAuthenticationProvider : IExternalAuthenticationProvider
    {
        private readonly GoogleExternalAuthSettings _googleExternalAuthSettings;

        public GoogleAuthenticationProvider(GoogleExternalAuthSettings googleExternalAuthSettings)
        {
            _googleExternalAuthSettings = googleExternalAuthSettings;
        }

        public string ConfigurationUrl => GoogleAuthenticationDefaults.ConfigurationUrl;

        public string SystemName => GoogleAuthenticationDefaults.ProviderSystemName;

        public string FriendlyName => "Google authentication";

        public int Priority => _googleExternalAuthSettings.DisplayOrder;

        public IList<string> LimitedToStores => new List<string>();

        public IList<string> LimitedToGroups => new List<string>();

        /// <summary>
        /// Gets a view component for displaying plugin in public store
        /// </summary>
        /// <param name="viewComponentName">View component name</param>
        public async Task<string> GetPublicViewComponentName()
        {
            return await Task.FromResult("GoogleAuthentication");
        }

    }
}
