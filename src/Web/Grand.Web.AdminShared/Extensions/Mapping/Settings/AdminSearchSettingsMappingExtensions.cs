using Grand.Domain.Admin;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Settings;

namespace Grand.Web.AdminShared.Extensions.Mapping.Settings;

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