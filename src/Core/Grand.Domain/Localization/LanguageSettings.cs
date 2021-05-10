using Grand.Domain.Configuration;

namespace Grand.Domain.Localization
{
    public class LanguageSettings : ISettings
    {
        /// <summary>
        /// Default admin area language identifier
        /// </summary>
        public string DefaultAdminLanguageId { get; set; }

        /// <summary>
        /// A value indicating whether we should detect the current language by a customer region (browser settings)
        /// </summary>
        public bool AutomaticallyDetectLanguage { get; set; }
      
        /// <summary>
        /// A value indicating whether to we should ignore RTL language property for admin area
        /// </summary>
        public bool IgnoreRtlPropertyForAdminArea { get; set; }
    }
}