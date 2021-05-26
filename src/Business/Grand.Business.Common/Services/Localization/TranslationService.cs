using Grand.Business.Common.Interfaces.Localization;
using Grand.Domain.Data;
using Grand.Domain.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Grand.Business.Common.Services.Localization
{
    /// <summary>
    /// Provides information about translations
    /// </summary>
    public partial class TranslationService : ITranslationService
    {
        #region Constants

        private Dictionary<string, TranslationResource> _allTranslateResource = null;

        #endregion

        #region Fields

        private readonly IRepository<TranslationResource> _translationRepository;
        private readonly IWorkContext _workContext;
        private readonly ICacheBase _cacheBase;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="cacheBase">Cache manager</param>
        /// <param name="workContext">Work context</param>
        /// <param name="trRepository">Translate resource repository</param>
        /// <param name="mediator">Mediator</param>
        public TranslationService(
            ICacheBase cacheBase,
            IWorkContext workContext,
            IRepository<TranslationResource> trRepository,
            IMediator mediator)
        {
            _cacheBase = cacheBase;
            _workContext = workContext;
            _translationRepository = trRepository;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a translate resource
        /// </summary>
        /// <param name="translateResourceId">Translate resource identifier</param>
        /// <returns>Translate resource</returns>
        public virtual Task<TranslationResource> GetTranslateResourceById(string translateResourceId)
        {
            return _translationRepository.GetByIdAsync(translateResourceId);
        }

        /// <summary>
        /// Gets a translate resource
        /// </summary>
        /// <param name="Name">A string representing a name</param>
        /// <param name="languageId">Language identifier</param>
        /// <returns>Translate resource</returns>
        public virtual async Task<TranslationResource> GetTranslateResourceByName(string name, string languageId)
        {
            var query = from lsr in _translationRepository.Table
                        orderby lsr.Name
                        where lsr.LanguageId == languageId && lsr.Name == name
                        select lsr;
            var translateResource = await Task.FromResult(query.FirstOrDefault());
            return translateResource;
        }

        /// <summary>
        /// Gets all translate resources by language identifier
        /// </summary>
        /// <param name="languageId">Language identifier</param>
        /// <returns>Translate resources</returns>
        public virtual IList<TranslationResource> GetAllResources(string languageId)
        {
            return _translationRepository.Table.Where(x=>x.LanguageId == languageId).ToList();
        }

        /// <summary>
        /// Inserts a translate resource
        /// </summary>
        /// <param name="translateResource">Translate resource</param>
        public virtual async Task InsertTranslateResource(TranslationResource translateResource)
        {
            if (translateResource == null)
                throw new ArgumentNullException(nameof(translateResource));

            translateResource.Name = translateResource.Name.ToLowerInvariant();
            await _translationRepository.InsertAsync(translateResource);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.TRANSLATERESOURCES_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(translateResource);
        }

        /// <summary>
        /// Updates the translate resource
        /// </summary>
        /// <param name="translateResource">Translate resource</param>
        public virtual async Task UpdateTranslateResource(TranslationResource translateResource)
        {
            if (translateResource == null)
                throw new ArgumentNullException(nameof(translateResource));

            translateResource.Name = translateResource.Name.ToLowerInvariant();
            await _translationRepository.UpdateAsync(translateResource);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.TRANSLATERESOURCES_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(translateResource);
        }


        /// <summary>
        /// Deletes a translate resource
        /// </summary>
        /// <param name="translateResource">Translate resource</param>
        public virtual async Task DeleteTranslateResource(TranslationResource translateResource)
        {
            if (translateResource == null)
                throw new ArgumentNullException(nameof(translateResource));

            await _translationRepository.DeleteAsync(translateResource);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.TRANSLATERESOURCES_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(translateResource);
        }

        /// <summary>
        /// Gets a resource string based on the specified ResourceKey property.
        /// </summary>
        /// <param name="name">A string representing a name.</param>
        /// <returns>A string representing the requested resource string.</returns>
        public virtual string GetResource(string name)
        {
            if (_workContext.WorkingLanguage != null)
                return GetResource(name, _workContext.WorkingLanguage.Id);

            return "";
        }

        /// <summary>
        /// Gets a resource string based on the specified ResourceKey property.
        /// </summary>
        /// <param name="name">A string representing a name.</param>
        /// <param name="languageId">Language identifier</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns>A string representing the requested resource string.</returns>
        public virtual string GetResource(string name, string languageId, string defaultValue = "", bool returnEmptyIfNotFound = false)
        {
            string result = string.Empty;
            if (name == null)
                name = string.Empty;
            name = name.Trim().ToLowerInvariant();

            if (_allTranslateResource != null)
            {
                if (_allTranslateResource.ContainsKey(name))
                    result = _allTranslateResource[name].Value;
            }
            else
            {
                string key = string.Format(CacheKey.TRANSLATERESOURCES_ALL_KEY, languageId);
                _allTranslateResource = _cacheBase.Get(key, () =>
                {
                    var dictionary = new Dictionary<string, TranslationResource>();
                    var locales = GetAllResources(languageId);
                    foreach (var locale in locales)
                    {
                        var resourceName = locale.Name.ToLowerInvariant();
                        if (!dictionary.ContainsKey(resourceName))
                            dictionary.Add(resourceName.ToLowerInvariant(), locale);
                        else
                        {
                            _translationRepository.Delete(locale);
                        }
                    }
                    return dictionary;
                });
                if (_allTranslateResource.ContainsKey(name))
                    result = _allTranslateResource[name].Value;
            }

            if (string.IsNullOrEmpty(result))
            {
                if (!string.IsNullOrEmpty(defaultValue))
                {
                    result = defaultValue;
                }
                else
                {
                    if (!returnEmptyIfNotFound)
                        result = name;
                }
            }
            return result;
        }

        /// <summary>
        /// Export language resources to xml
        /// </summary>
        /// <param name="language">Language</param>
        /// <returns>Result in XML format</returns>
        public virtual async Task<string> ExportResourcesToXml(Language language)
        {
            if (language == null)
                throw new ArgumentNullException(nameof(language));
            var sb = new StringBuilder();

            var xwSettings = new XmlWriterSettings
            {
                ConformanceLevel = ConformanceLevel.Auto,
                Async = true
            };

            using (var stringWriter = new StringWriter(sb))
            using (var xmlWriter = XmlWriter.Create(stringWriter, xwSettings))
            {

                await xmlWriter.WriteStartDocumentAsync();
                xmlWriter.WriteStartElement("Language");
                xmlWriter.WriteAttributeString("Name", language.Name);

                var resources = GetAllResources(language.Id);
                foreach (var resource in resources)
                {
                    xmlWriter.WriteStartElement("Resource");
                    xmlWriter.WriteAttributeString("Name", resource.Name);
                    xmlWriter.WriteElementString("Value", null, resource.Value);
                    xmlWriter.WriteEndElement();
                }

                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndDocument();
                await xmlWriter.FlushAsync();
                return stringWriter.ToString();
            }
        }

        /// <summary>
        /// Import language resources from XML file
        /// </summary>
        /// <param name="language">Language</param>
        /// <param name="xml">XML</param>
        public virtual async Task ImportResourcesFromXml(Language language, string xml)
        {
            if (language == null)
                throw new ArgumentNullException(nameof(language));

            if (string.IsNullOrEmpty(xml))
                return;

            var translateResources = new List<TranslationResource>();
            //stored procedures aren't supported
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);

            var nodes = xmlDoc.SelectNodes(@"//Language/Resource");
            foreach (XmlNode node in nodes)
            {
                string name = node.Attributes["Name"].InnerText.Trim();
                string value = "";
                var valueNode = node.SelectSingleNode("Value");
                if (valueNode != null)
                    value = valueNode.InnerText;

                if (String.IsNullOrEmpty(name))
                    continue;

                //bulk insert
                var resource = (from l in _translationRepository.Table
                                      where l.Name == name.ToLowerInvariant() && l.LanguageId == language.Id
                                      select l).FirstOrDefault();

                if (resource != null)
                {
                    resource.Name = resource.Name.ToLowerInvariant();
                    resource.Value = value;
                    await _translationRepository.UpdateAsync(resource);
                }
                else
                {

                    translateResources.Add(new TranslationResource
                    {
                        LanguageId = language.Id,
                        Name = name.ToLowerInvariant(),
                        Value = value
                    });
                }
            }

            if (translateResources.Any())
                await _translationRepository.InsertManyAsync(translateResources);


            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.TRANSLATERESOURCES_PATTERN_KEY);
        }

        /// <summary>
        /// Import language resources from XML file
        /// </summary>
        /// <param name="language">Language</param>
        /// <param name="xml">XML</param>
        public virtual async Task ImportResourcesFromXmlInstall(Language language, string xml)
        {
            if (language == null)
                throw new ArgumentNullException(nameof(language));

            if (String.IsNullOrEmpty(xml))
                return;

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);

            var translateResources = new List<TranslationResource>();

            var nodes = xmlDoc.SelectNodes(@"//Language/Resource");
            foreach (XmlNode node in nodes)
            {
                string name = node.Attributes["Name"].InnerText.Trim();
                string value = "";
                var valueNode = node.SelectSingleNode("Value");
                if (valueNode != null)
                    value = valueNode.InnerText;

                if (string.IsNullOrEmpty(name))
                    continue;

                translateResources.Add(
                    new TranslationResource
                    {
                        LanguageId = language.Id,
                        Name = name.ToLowerInvariant(),
                        Value = value
                    });
            }

            await _translationRepository.InsertManyAsync(translateResources);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.TRANSLATERESOURCES_PATTERN_KEY);
        }

        #endregion
    }
}
