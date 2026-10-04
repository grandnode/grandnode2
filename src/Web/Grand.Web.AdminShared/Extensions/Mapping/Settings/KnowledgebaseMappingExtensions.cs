using Grand.Domain.Knowledgebase;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Settings;

namespace Grand.Web.AdminShared.Extensions.Mapping.Settings;

public static class KnowledgebaseSettingsMappingExtensions
{
    public static ContentSettingsModel.KnowledgebaseSettingsModel ToModel(this KnowledgebaseSettings entity)
    {
        return entity.MapTo<KnowledgebaseSettings, ContentSettingsModel.KnowledgebaseSettingsModel>();
    }

    public static KnowledgebaseSettings ToEntity(this ContentSettingsModel.KnowledgebaseSettingsModel model,
        KnowledgebaseSettings destination)
    {
        return model.MapTo(destination);
    }
}