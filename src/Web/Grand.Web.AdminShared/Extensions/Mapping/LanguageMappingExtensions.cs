using Grand.Domain.Localization;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Localization;

namespace Grand.Web.AdminShared.Extensions.Mapping;

public static class LanguageMappingExtensions
{
    public static LanguageModel ToModel(this Language entity)
    {
        return entity.MapTo<Language, LanguageModel>();
    }

    public static Language ToEntity(this LanguageModel model)
    {
        return model.MapTo<LanguageModel, Language>();
    }

    public static Language ToEntity(this LanguageModel model, Language destination)
    {
        return model.MapTo(destination);
    }
}