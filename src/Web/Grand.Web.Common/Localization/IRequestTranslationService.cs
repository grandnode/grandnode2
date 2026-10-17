using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Localization;
using Grand.Infrastructure;

namespace Grand.Web.Common.Localization;

/// <summary>
///     Translation service for code that runs inside a request
/// </summary>
public interface IRequestTranslationService : ITranslationService
{
    /// <summary>
    ///     Gets a resource string in the working language of the current request.
    /// </summary>
    /// <param name="name">A string representing a name.</param>
    /// <returns>A string representing the requested resource string.</returns>
    string GetResource(string name);
}

public class RequestTranslationService(ITranslationService translationService, IContextAccessor contextAccessor)
    : IRequestTranslationService
{
    public string GetResource(string name)
    {
        var language = contextAccessor.WorkContext.WorkingLanguage;
        return language != null ? translationService.GetResource(name, language.Id) : "";
    }

    public string GetResource(string resourceKey, string languageId, string defaultValue = "",
        bool returnEmptyIfNotFound = false)
    {
        return translationService.GetResource(resourceKey, languageId, defaultValue, returnEmptyIfNotFound);
    }

    public Task<TranslationResource> GetTranslateResourceById(string translateResourceId)
    {
        return translationService.GetTranslateResourceById(translateResourceId);
    }

    public Task<TranslationResource> GetTranslateResourceByName(string name, string languageId)
    {
        return translationService.GetTranslateResourceByName(name, languageId);
    }

    public IList<TranslationResource> GetAllResources(string languageId)
    {
        return translationService.GetAllResources(languageId);
    }

    public Task InsertTranslateResource(TranslationResource translateResource)
    {
        return translationService.InsertTranslateResource(translateResource);
    }

    public Task UpdateTranslateResource(TranslationResource translateResource)
    {
        return translationService.UpdateTranslateResource(translateResource);
    }

    public Task DeleteTranslateResource(TranslationResource translateResource)
    {
        return translationService.DeleteTranslateResource(translateResource);
    }

    public Task<string> ExportResourcesToXml(Language language)
    {
        return translationService.ExportResourcesToXml(language);
    }

    public Task ImportResourcesFromXml(Language language, string xml)
    {
        return translationService.ImportResourcesFromXml(language, xml);
    }

    public Task ImportResourcesFromXmlInstall(Language language, string xml)
    {
        return translationService.ImportResourcesFromXmlInstall(language, xml);
    }
}
