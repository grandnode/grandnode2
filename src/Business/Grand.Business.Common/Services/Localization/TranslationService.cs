using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Data;
using Grand.Domain.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel.Extensions;
using MediatR;
using System.Collections.Concurrent;
using System.Xml;

namespace Grand.Business.Common.Services.Localization;

/// <summary>
///     Provides information about translations
/// </summary>
public class TranslationService : ITranslationService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="workContext">Work context</param>
    /// <param name="trRepository">Translate resource repository</param>
    /// <param name="mediator">Mediator</param>
    public TranslationService(
        IContextAccessor contextAccessor,
        IRepository<TranslationResource> trRepository,
        IMediator mediator)
    {
        _contextAccessor = contextAccessor;
        _translationRepository = trRepository;
        _mediator = mediator;
    }

    #endregion

    #region Fields

    private static readonly ConcurrentDictionary<string, IDictionary<string, string>> _cachedResources = new();

    private readonly IRepository<TranslationResource> _translationRepository;
    private readonly IContextAccessor _contextAccessor;
    private readonly IMediator _mediator;

    #endregion

    #region Methods

    /// <summary>
    ///     Gets a translate resource
    /// </summary>
    /// <param name="translateResourceId">Translate resource identifier</param>
    /// <returns>Translate resource</returns>
    public virtual Task<TranslationResource> GetTranslateResourceById(string translateResourceId)
    {
        return _translationRepository.GetByIdAsync(translateResourceId);
    }

    /// <summary>
    ///     Gets a translate resource
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
    ///     Gets all translate resources by language identifier
    /// </summary>
    /// <param name="languageId">Language identifier</param>
    /// <returns>Translate resources</returns>
    public virtual IList<TranslationResource> GetAllResources(string languageId)
    {
        return _translationRepository.Table.Where(x => x.LanguageId == languageId).ToList();
    }

    /// <summary>
    ///     Inserts a translate resource
    /// </summary>
    /// <param name="translateResource">Translate resource</param>
    public virtual async Task InsertTranslateResource(TranslationResource translateResource)
    {
        ArgumentNullException.ThrowIfNull(translateResource);

        translateResource.Name = translateResource.Name.ToLowerInvariant();
        await _translationRepository.InsertAsync(translateResource);

        await RefreshCachedResources(translateResource.LanguageId);

        //event notification
        await _mediator.EntityInserted(translateResource);
    }

    /// <summary>
    ///     Updates the translate resource
    /// </summary>
    /// <param name="translateResource">Translate resource</param>
    public virtual async Task UpdateTranslateResource(TranslationResource translateResource)
    {
        ArgumentNullException.ThrowIfNull(translateResource);

        translateResource.Name = translateResource.Name.ToLowerInvariant();
        await _translationRepository.UpdateAsync(translateResource);

        //cache
        await RefreshCachedResources(translateResource.LanguageId);

        //event notification
        await _mediator.EntityUpdated(translateResource);
    }


    /// <summary>
    ///     Deletes a translate resource
    /// </summary>
    /// <param name="translateResource">Translate resource</param>
    public virtual async Task DeleteTranslateResource(TranslationResource translateResource)
    {
        ArgumentNullException.ThrowIfNull(translateResource);

        await _translationRepository.DeleteAsync(translateResource);

        //cache
        await RefreshCachedResources(translateResource.LanguageId);

        //event notification
        await _mediator.EntityDeleted(translateResource);
    }

    /// <summary>
    ///     Gets a resource string based on the specified ResourceKey property.
    /// </summary>
    /// <param name="name">A string representing a name.</param>
    /// <returns>A string representing the requested resource string.</returns>
    public virtual string GetResource(string name)
    {
        return _contextAccessor.WorkContext.WorkingLanguage != null ? GetResource(name, _contextAccessor.WorkContext.WorkingLanguage.Id) : "";
    }

    /// <summary>
    ///     Gets a resource string based on the specified ResourceKey property.
    /// </summary>
    /// <param name="name">A string representing a name.</param>
    /// <param name="languageId">Language identifier</param>
    /// <param name="defaultValue">Default value</param>
    /// <param name="returnEmptyIfNotFound">
    ///     A value indicating whether an empty string will be returned if a resource is not
    ///     found and default value is set to empty string
    /// </param>
    /// <returns>A string representing the requested resource string.</returns>
    public virtual string GetResource(string name, string languageId, string defaultValue = "",
        bool returnEmptyIfNotFound = false)
    {
        name ??= string.Empty;
        name = name.Trim().ToLowerInvariant();

        var result = GetOrAddToStaticCache(languageId, name);

        if (!string.IsNullOrEmpty(result))
            return result;

        if (!string.IsNullOrEmpty(defaultValue)) result = defaultValue;
        else if (!returnEmptyIfNotFound) result = name;
        return result;
    }

    /// <summary>
    ///     Export language resources to xml
    /// </summary>
    /// <param name="language">Language</param>
    /// <returns>Result in XML format</returns>
    public virtual async Task<string> ExportResourcesToXml(Language language)
    {
        ArgumentNullException.ThrowIfNull(language);
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
    ///     Import language resources from XML file
    /// </summary>
    /// <param name="language">Language</param>
    /// <param name="xml">XML</param>
    public virtual async Task ImportResourcesFromXml(Language language, string xml)
    {
        ArgumentNullException.ThrowIfNull(language);

        if (string.IsNullOrEmpty(xml))
            return;

        var xmlDoc = XmlExtensions.LanguageXmlDocument(xml);

        var translateResources = XmlExtensions.ParseTranslationResources(xmlDoc);

        foreach (var item in translateResources)
        {
            //bulk insert
            var resource = (from l in _translationRepository.Table
                            where l.Name == item.Name.ToLowerInvariant() && l.LanguageId == language.Id
                            select l).FirstOrDefault();

            if (resource != null)
            {
                resource.Name = resource.Name.ToLowerInvariant();
                resource.Value = item.Value;
                if (Enum.TryParse<TranslationResourceArea>(item.Area, out var areaEnum))
                    resource.Area = areaEnum;
                await _translationRepository.UpdateAsync(resource);
            }
            else
            {
                _ = Enum.TryParse(item.Area, out TranslationResourceArea areaEnum);
                await _translationRepository.InsertAsync(new TranslationResource {
                    LanguageId = language.Id,
                    Name = item.Name.ToLowerInvariant(),
                    Value = item.Value,
                    Area = areaEnum,
                    CreatedBy = _contextAccessor.WorkContext.CurrentCustomer.Email
                });
            }
        }
        await RefreshCachedResources(language.Id);
    }

    /// <summary>
    ///     Import language resources from XML file
    /// </summary>
    /// <param name="language">Language</param>
    /// <param name="xml">XML</param>
    public virtual async Task ImportResourcesFromXmlInstall(Language language, string xml)
    {
        ArgumentNullException.ThrowIfNull(language);

        if (string.IsNullOrEmpty(xml))
            return;

        var xmlDoc = XmlExtensions.LanguageXmlDocument(xml);

        var translateResources = XmlExtensions.ParseTranslationResources(xmlDoc);

        foreach (var item in translateResources)
        {
            _ = Enum.TryParse(item.Area, out TranslationResourceArea areaEnum);

            await _translationRepository.InsertAsync(new TranslationResource {
                LanguageId = language.Id,
                Name = item.Name.ToLowerInvariant(),
                Value = item.Value,
                Area = areaEnum,
                CreatedBy = "System"
            });
        }
        await RefreshCachedResources(language.Id);
    }

    private string GetOrAddToStaticCache(string languageId, string name)
    {
        if (_cachedResources.TryGetValue(languageId, out var languageResources))
        {
            languageResources.TryGetValue(name, out var cachedResult);
            return cachedResult;
        }

        var value = CreateLanguageResourcesDictionary(languageId);
        _cachedResources.TryAdd(languageId, value);
        value.TryGetValue(name, out var result);
        return result;
    }

    private Task RefreshCachedResources(string languageId)
    {
        if (_cachedResources.ContainsKey(languageId)) _cachedResources.TryRemove(languageId, out _);
        _cachedResources.TryAdd(languageId, CreateLanguageResourcesDictionary(languageId));

        return Task.CompletedTask;
    }

    private Dictionary<string, string> CreateLanguageResourcesDictionary(string languageId)
    {
        return GetAllResources(languageId)
            .GroupBy(r => r.Name)
            .ToDictionary(
                g => g.Key,
                g => g.First().Value);
    }

    #endregion
}