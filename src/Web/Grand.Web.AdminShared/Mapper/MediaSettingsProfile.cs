using AutoMapper;
using Grand.Domain.Media;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Settings;

namespace Grand.Web.AdminShared.Mapper;

public class MediaSettingsProfile : Profile, IAutoMapperProfile
{
    public MediaSettingsProfile()
    {
        CreateMap<MediaSettings, MediaSettingsModel>()
            .ForMember(dest => dest.ActiveStore, mo => mo.Ignore())
            .ForMember(dest => dest.UserFields, mo => mo.Ignore());
        CreateMap<MediaSettingsModel, MediaSettings>();
    }

    public int Order => 0;
}