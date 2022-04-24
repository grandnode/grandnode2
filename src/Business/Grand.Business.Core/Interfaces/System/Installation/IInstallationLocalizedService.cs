using Grand.Business.Core.Utilities.System;

namespace Grand.Business.Core.Interfaces.System.Installation
{
    /// <summary>
    /// Translation service for installation process
    /// </summary>
    public partial interface IInstallationLocalizedService
    {
        /// <summary>
        /// Get locale resource value
        /// </summary>
        /// <param name="languageCode">Language code</param>
        /// <param name="resourceName">Resource name</param>
        /// <returns>Resource value</returns>
        string GetResource(string languageCode, string resourceName);

        /// <summary>
        /// Get current language for the installation page
        /// </summary>
        /// <returns>Current language</returns>
        /// <param name="languageCode">Language Code</param>
        InstallationLanguage GetCurrentLanguage(string languageCode = default);

        /// <summary>
        /// Get a list of available languages
        /// </summary>
        /// <returns>Available installation languages</returns>
        IList<InstallationLanguage> GetAvailableLanguages();

        /// <summary>
        /// Get a list of available collactions
        /// </summary>
        /// <returns>Available collations mongodb</returns>
        IList<InstallationCollation> GetAvailableCollations();
    }
}
