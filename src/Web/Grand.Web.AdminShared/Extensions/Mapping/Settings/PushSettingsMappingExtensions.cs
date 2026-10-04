using Grand.Domain.PushNotifications;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Settings;

namespace Grand.Web.AdminShared.Extensions.Mapping.Settings;

public static class PushSettingsMappingExtensions
{
    public static PushNotificationsSettingsModel ToModel(this PushNotificationsSettings entity)
    {
        return entity.MapTo<PushNotificationsSettings, PushNotificationsSettingsModel>();
    }

    public static PushNotificationsSettings ToEntity(this PushNotificationsSettingsModel model,
        PushNotificationsSettings destination)
    {
        return model.MapTo(destination);
    }
}