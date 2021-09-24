using AutoMapper;
using Grand.Domain.PushNotifications;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Mapper
{
    public class PushNotificationsSettingsProfile : Profile, IAutoMapperProfile
    {
        public PushNotificationsSettingsProfile()
        {
            CreateMap<PushNotificationsSettings, PushNotificationsSettingsModel>()
                .ForMember(dest => dest.PushApiKey, mo => mo.MapFrom(y=>y.PublicApiKey))
                .ForMember(dest => dest.UserFields, mo => mo.Ignore());

            CreateMap<PushNotificationsSettingsModel, PushNotificationsSettings>()
                .ForMember(dest => dest.PublicApiKey, mo => mo.MapFrom(y => y.PushApiKey));
        }

        public int Order => 0;
    }
}