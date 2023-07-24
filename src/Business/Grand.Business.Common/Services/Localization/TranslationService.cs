using Grand.Business.Common.Utilities;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Data;
using Grand.Domain.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using MediatR;
using System.Xml;
using System.Xml.Schema;

namespace Grand.Business.Common.Services.Localization
{
    /// <summary>
    /// Provides information about translations
    /// </summary>
    public class TranslationService : ITranslationService
    {
        #region Constants

        private Dictionary<string, string> _allTranslateResource;

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
        /// <param name="name">A string representing a name</param>
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
            return _translationRepository.Table.Where(x => x.LanguageId == languageId).ToList();
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
            return _workContext.WorkingLanguage != null ? GetResource(name, _workContext.WorkingLanguage.Id) : "";
        }

        /// <summary>
        /// Gets a resource string based on the specified ResourceKey property.
        /// </summary>
        /// <param name="name">A string representing a name.</param>
        /// <param name="languageId">Language identifier</param>
        /// <param name="defaultValue">Default value</param>
        /// <param name="returnEmptyIfNotFound">A value indicating whether an empty string will be returned if a resource is not found and default value is set to empty string</param>
        /// <returns>A string representing the requested resource string.</returns>
        public virtual string GetResource(string name, string languageId, string defaultValue = "", bool returnEmptyIfNotFound = false)
        {
            string result;
            name ??= string.Empty;

            name = name.Trim().ToLowerInvariant();
            if (_allTranslateResource != null)
            {
                _allTranslateResource.TryGetValue(name, out result);
            }
            else
            {
                var key = string.Format(CacheKey.TRANSLATERESOURCES_ALL_KEY, languageId);
                _allTranslateResource = _cacheBase.Get(key, () =>
                {
                    return
                        GetAllResources(languageId)
                            .GroupBy(r => r.Name)
                            .ToDictionary(
                                keySelector: g => g.Key,
                                elementSelector: g => g.First().Value);
                });
                _allTranslateResource.TryGetValue(name, out result);
            }

            if (!string.IsNullOrEmpty(result)) return result;
            if (!string.IsNullOrEmpty(defaultValue)) result = defaultValue;
            else if (!returnEmptyIfNotFound) result = name;
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

            var xwSettings = new XmlWriterSettings {
                ConformanceLevel = ConformanceLevel.Auto,
                Async = true
            };

            await using var stringWriter = new StringWriter(sb);
            await using var xmlWriter = XmlWriter.Create(stringWriter, xwSettings);
            await xmlWriter.WriteStartDocumentAsync();
            xmlWriter.WriteStartElement("Language");
            xmlWriter.WriteAttributeString("Name", language.Name);

            var resources = GetAllResources(language.Id);
            foreach (var resource in resources)
            {
                xmlWriter.WriteStartElement("Resource");
                xmlWriter.WriteAttributeString("Name", resource.Name);
                xmlWriter.WriteAttributeString("Area", resource.Area.ToString());
                xmlWriter.WriteElementString("Value", null, resource.Value);
                await xmlWriter.WriteEndElementAsync();
            }

            await xmlWriter.WriteEndElementAsync();
            await xmlWriter.WriteEndDocumentAsync();
            await xmlWriter.FlushAsync();
            return stringWriter.ToString();
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
           
            var xmlDoc = LanguageXmlDocument(xml);

            var nodes = xmlDoc.SelectNodes(@"//Language/Resource");
            if (nodes != null)
                foreach (XmlNode node in nodes)
                {
                    var name = node.Attributes?["Name"]?.InnerText.Trim();
                    var area = node.Attributes?["Area"]?.InnerText.Trim();
                    var value = "";
                    var valueNode = node.SelectSingleNode("Value");
                    if (valueNode != null)
                        value = valueNode.InnerText;

                    if (string.IsNullOrEmpty(name))
                        continue;

                    //bulk insert
                    var resource = (from l in _translationRepository.Table
                        where l.Name == name.ToLowerInvariant() && l.LanguageId == language.Id
                        select l).FirstOrDefault();

                    if (resource != null)
                    {
                        resource.Name = resource.Name.ToLowerInvariant();
                        resource.Value = value;
                        if (Enum.TryParse<TranslationResourceArea>(area, out var areaEnum))
                            resource.Area = areaEnum;
                        await _translationRepository.UpdateAsync(resource);
                    }
                    else
                    {
                        _ = Enum.TryParse(area, out TranslationResourceArea areaEnum);

                        translateResources.Add(new TranslationResource {
                            LanguageId = language.Id,
                            Name = name.ToLowerInvariant(),
                            Value = value,
                            Area = areaEnum
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

            if (string.IsNullOrEmpty(xml))
                return;
            
            var xmlDoc = LanguageXmlDocument(xml);

            var translateResources = new List<TranslationResource>();

            var nodes = xmlDoc.SelectNodes(@"//Language/Resource");
            if (nodes != null)
                foreach (XmlNode node in nodes)
                {
                    var name = node.Attributes?["Name"]?.InnerText.Trim();
                    var area = node.Attributes?["Area"]?.InnerText.Trim();
                    var value = "";
                    var valueNode = node.SelectSingleNode("Value");
                    if (valueNode != null)
                        value = valueNode.InnerText;

                    if (string.IsNullOrEmpty(name))
                        continue;

                    _ = Enum.TryParse(area, out TranslationResourceArea areaEnum);

                    translateResources.Add(
                        new TranslationResource {
                            LanguageId = language.Id,
                            Name = name.ToLowerInvariant(),
                            Value = value,
                            Area = areaEnum
                        });
                }

            await _translationRepository.InsertManyAsync(translateResources);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.TRANSLATERESOURCES_PATTERN_KEY);
        }
        private static XmlDocument LanguageXmlDocument(string xml)
        {
            var schemas = new XmlSchemaSet();
            schemas.Add("", XmlReader.Create(new StringReader(LanguageSchema.SchemaXsd)));

            var xmlDoc = new XmlDocument { Schemas = schemas, XmlResolver = null };
            xmlDoc.LoadXml(xml);

            // Validate XML.
            xmlDoc.Validate((_, e) => throw new XmlException("XML data does not conform to the schema", e.Exception));
            return xmlDoc;
        }

        #endregion
    }
}
