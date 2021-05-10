using Grand.Domain.Localization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Common.Interfaces.Localization
{
    /// <summary>
    /// Translation manager interface
    /// </summary>
    public partial interface ITranslationService
    {
        
        /// <summary>
        /// Gets a translate resource
        /// </summary>
        /// <param name="translateResourceId">Translate resource identifier</param>
        /// <returns>Translate resource</returns>
        Task<TranslationResource> GetTranslateResourceById(string translateResourceId);

        /// <summary>
        /// Gets a translate resource
        /// </summary>
        /// <param name="name">A string representing a resource name</param>
        /// <param name="languageId">Language identifier</param>
        /// <returns>Translate resource</returns>
        Task<TranslationResource> GetTranslateResourceByName(string name, string languageId);

        /// <summary>
        /// Gets all translate resources by language identifier
        /// </summary>
        /// <param name="languageId">Language identifier</param>
        /// <returns>Translate resources</returns>
        IList<TranslationResource> GetAllResources(string languageId);

        /// <summary>
        /// Inserts a translate resource
        /// </summary>
        /// <param name="translateResource">Translate resource</param>
        Task InsertTranslateResource(TranslationResource translateResource);

        /// <summary>
        /// Updates the translate resource
        /// </summary>
        /// <param name="translateResource">Translate resource</param>
        Task UpdateTranslateResource(TranslationResource translateResource);

        /// <summary>
        /// Deletes a translate resource
        /// </summary>
        /// <param name="translateResource">Translate resource</param>
        Task DeleteTranslateResource(TranslationResource translateResource);

        /// <summary>
        /// Gets a resource string based on the specified ResourceKey property.
        /// </summary>
        /// <param name="name">A string representing a name.</param>
        /// <returns>A string representing the requested resource string.</returns>
        string GetResource(string name);

        /// <summary>
        /// Gets a resource string based on the specified ResourceKey property.
        /// </summary>
        /// <param name="resourceKey">A string representing a ResourceKey.</param>
        /// <param name="languageId">Language identifier</param>
        /// <param name="defaultValue">Default value</param>
        /// <param name="returnEmptyIfNotFound">A value indicating whether an empty string will be returned if a resource is not found and default value is set to empty string</param>
        /// <returns>A string representing the requested resource string.</returns>
        string GetResource(string resourceKey, string languageId, string defaultValue = "", bool returnEmptyIfNotFound = false);

        /// <summary>
        /// Export language resources to xml
        /// </summary>
        /// <param name="language">Language</param>
        /// <returns>Result in XML format</returns>
        Task<string> ExportResourcesToXml(Language language);

        /// <summary>
        /// Import language resources from XML file
        /// </summary>
        /// <param name="language">Language</param>
        /// <param name="xml">XML</param>
        Task ImportResourcesFromXml(Language language, string xml);

        /// <summary>
        /// Import language resources from XML file
        /// </summary>
        /// <param name="language">Language</param>
        /// <param name="xml">XML</param>
        Task ImportResourcesFromXmlInstall(Language language, string xml);
    }
}
