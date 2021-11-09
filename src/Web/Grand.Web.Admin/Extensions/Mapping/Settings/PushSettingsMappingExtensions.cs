using Grand.Domain.PushNotifications;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Extensions
{
    public static class PushSettingsMappingExtensions
    {
        public static PushNotificationsSettingsModel ToModel(this PushNotificationsSettings entity)
        {
            return entity.MapTo<PushNotificationsSettings, PushNotificationsSettingsModel>();
        }
        public static PushNotificationsSettings ToEntity(this PushNotificationsSettingsModel model, PushNotificationsSettings destination)
        {
            return model.MapTo(destination);
        }
    }
}