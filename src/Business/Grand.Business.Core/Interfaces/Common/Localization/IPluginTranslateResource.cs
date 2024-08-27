using Grand.Domain.Localization;

namespace Grand.Business.Core.Interfaces.Common.Localization;

public interface IPluginTranslateResource
{
    Task AddOrUpdatePluginTranslateResource(string name, string value, TranslationResourceArea area = TranslationResourceArea.Common, string languageCulture = null);
    Task DeletePluginTranslationResource(string name);
}