using Grand.Business.Core.Interfaces.System.Installation;
using Grand.Business.Core.Utilities.System;
using Grand.SharedKernel.Extensions;
using System.Text.RegularExpressions;
using System.Xml;

namespace Grand.Business.System.Services.Installation
{
    /// <summary>
    /// Translation service for installation process
    /// </summary>
    public partial class InstallationLocalizedService : IInstallationLocalizedService
    {
        /// <summary>
        /// Available languages
        /// </summary>
        private IList<InstallationLanguage> _availableLanguages;

        /// <summary>
        /// Available collation
        /// </summary>
        private IList<InstallationCollation> _availableCollation;


        public InstallationLocalizedService()
        {

        }
        /// <summary>
        /// Get locale resource value
        /// </summary>
        /// <param name="languageCode">Language code</param>
        /// <param name="resourceName">Resource name</param>
        /// <returns>Resource value</returns>
        public string GetResource(string languageCode, string resourceName)
        {
            var language = GetCurrentLanguage(languageCode);
            if (language == null)
                return resourceName;
            var resourceValue = language.Resources
                .Where(r => r.Name.Equals(resourceName, StringComparison.OrdinalIgnoreCase))
                .Select(r => r.Value)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(resourceValue))
                //return name
                return resourceName;

            return resourceValue;
        }

        /// <summary>
        /// Get current language for the installation page
        /// </summary>
        /// <returns>Current language</returns>
        /// <param name="languageCode">Language Code</param>
        public virtual InstallationLanguage GetCurrentLanguage(string languageCode = default)
        {
            var availableLanguages = GetAvailableLanguages();
            if(!string.IsNullOrEmpty(languageCode))
            {
                var selectedlanguage = availableLanguages
                    .FirstOrDefault(l => l.Code.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
                if (selectedlanguage != null)
                    return selectedlanguage;
            }

            //return the default one
            var language = availableLanguages.FirstOrDefault(l => l.IsDefault);
            if (language != null)
                return language;

            //return any available language
            language = availableLanguages.FirstOrDefault();
            return language;
        }

        /// <summary>
        /// Get a list of available languages
        /// </summary>
        /// <returns>Available installation languages</returns>
        public virtual IList<InstallationLanguage> GetAvailableLanguages()
        {
            if (_availableLanguages != null)
                return _availableLanguages;

            _availableLanguages = new List<InstallationLanguage>();
            foreach (var filePath in Directory.EnumerateFiles(CommonPath.MapPath("App_Data/Resources/Installation/"), "*.xml"))
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.Load(File.OpenRead(filePath));

                var languageCode = "";
                var r = new Regex(Regex.Escape("installation.") + "(.*?)" + Regex.Escape(".xml"));
                var matches = r.Matches(Path.GetFileName(filePath));
                foreach (Match match in matches)
                    languageCode = match.Groups[1].Value;

                var languageNode = xmlDocument.SelectSingleNode(@"//Language");

                if (languageNode == null || languageNode.Attributes == null)
                    continue;

                //get language friendly name
                var languageName = languageNode.Attributes["Name"].InnerText.Trim();

                //is default
                var isDefaultAttribute = languageNode.Attributes["IsDefault"];
                var isDefault = isDefaultAttribute != null && Convert.ToBoolean(isDefaultAttribute.InnerText.Trim());

                //is default
                var isRightToLeftAttribute = languageNode.Attributes["IsRightToLeft"];
                var isRightToLeft = isRightToLeftAttribute != null && Convert.ToBoolean(isRightToLeftAttribute.InnerText.Trim());

                //create language
                var language = new InstallationLanguage {
                    Code = languageCode,
                    Name = languageName,
                    IsDefault = isDefault,
                    IsRightToLeft = isRightToLeft,
                };

                //load resources
                var resources = xmlDocument.SelectNodes(@"//Language/LocaleResource");
                if (resources == null)
                    continue;
                foreach (XmlNode resNode in resources)
                {
                    if (resNode.Attributes == null)
                        continue;

                    var resNameAttribute = resNode.Attributes["Name"];
                    var resValueNode = resNode.SelectSingleNode("Value");

                    if (resNameAttribute == null)
                        continue;
                    var resourceName = resNameAttribute.Value.Trim();
                    if (string.IsNullOrEmpty(resourceName))
                        continue;

                    if (resValueNode == null)
                        continue;
                    var resourceValue = resValueNode.InnerText.Trim();

                    language.Resources.Add(new InstallationLocaleResource {
                        Name = resourceName,
                        Value = resourceValue
                    });
                }

                _availableLanguages.Add(language);
                _availableLanguages = _availableLanguages.OrderBy(l => l.Name).ToList();

            }
            return _availableLanguages;
        }

        /// <summary>
        /// Get a list of available collactions
        /// </summary>
        /// <returns>Available collations mongodb</returns>
        public virtual IList<InstallationCollation> GetAvailableCollations()
        {
            if (_availableCollation != null)
                return _availableCollation;

            _availableCollation = new List<InstallationCollation>();
            var filePath = CommonPath.MapPath("App_Data/Resources/supportedcollation.xml");
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(File.OpenRead(filePath));

            var collation = xmlDocument.SelectNodes(@"//Collations/Collation");

            foreach (XmlNode resNode in collation)
            {
                var resNameAttribute = resNode.Attributes["Name"];
                var resValueNode = resNode.SelectSingleNode("Value");

                var resourceName = resNameAttribute.Value.Trim();
                var resourceValue = resValueNode.InnerText.Trim();

                _availableCollation.Add(new InstallationCollation() {
                    Name = resourceName,
                    Value = resourceValue,
                });
            }

            return _availableCollation;
        }
    }
}
