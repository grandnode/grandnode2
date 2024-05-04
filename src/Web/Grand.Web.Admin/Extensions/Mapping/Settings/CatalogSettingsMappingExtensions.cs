using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Extensions.Mapping.Settings;

public static class CatalogSettingsMappingExtensions
{
    public static CatalogSettingsModel ToModel(this CatalogSettings entity)
    {
        return entity.MapTo<CatalogSettings, CatalogSettingsModel>();
    }

    public static CatalogSettings ToEntity(this CatalogSettingsModel model, CatalogSettings destination)
    {
        return model.MapTo(destination);
    }
}