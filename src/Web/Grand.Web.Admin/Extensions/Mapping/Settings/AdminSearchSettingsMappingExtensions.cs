using Grand.Domain.Admin;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Extensions.Mapping.Settings;

public static class AdminSearchSettingsMappingExtensions
{
    public static AdminSearchSettingsModel ToModel(this AdminSearchSettings entity)
    {
        return entity.MapTo<AdminSearchSettings, AdminSearchSettingsModel>();
    }

    public static AdminSearchSettings ToEntity(this AdminSearchSettingsModel model, AdminSearchSettings destination)
    {
        return model.MapTo(destination);
    }
}