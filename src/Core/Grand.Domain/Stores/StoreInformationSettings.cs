using Grand.Domain.Configuration;

namespace Grand.Domain.Stores
{
    public class StoreInformationSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether store is closed
        /// </summary>
        public bool StoreClosed { get; set; }

        /// <summary>
        /// Gets or sets a default store theme
        /// </summary>
        public string DefaultStoreTheme { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether customers are allowed to select a theme
        /// </summary>
        public bool AllowCustomerToSelectTheme { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether customers are allowed to select a admin theme
        /// </summary>
        public bool AllowToSelectAdminTheme { get; set; }

        /// <summary>
        /// Gets or sets a logo
        /// </summary>
        public string LogoPicture { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether we should display info about the cookie
        /// </summary>
        public bool DisplayCookieInformation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether we should display privacy preference
        /// </summary>
        public bool DisplayPrivacyPreference { get; set; }

        /// <summary>
        /// Gets or sets a value of Facebook page URL of the site
        /// </summary>
        public string FacebookLink { get; set; }

        /// <summary>
        /// Gets or sets a value of Twitter page URL of the site
        /// </summary>
        public string TwitterLink { get; set; }

        /// <summary>
        /// Gets or sets a value of YouTube channel URL of the site
        /// </summary>
        public string YoutubeLink { get; set; }

        /// <summary>
        /// Gets or sets a value of Instagram page URL of the site
        /// </summary>
        public string InstagramLink { get; set; }

        /// <summary>
        /// Gets or sets a value of LinkedIn page URL of the site
        /// </summary>
        public string LinkedInLink { get; set; }

        /// <summary>
        /// Gets or sets a value of Pinterest page URL of the site
        /// </summary>
        public string PinterestLink { get; set; }
    }
}
