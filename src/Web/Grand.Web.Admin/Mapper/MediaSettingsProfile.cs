using AutoMapper;
using Grand.Domain.Media;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Mapper
{
    public class MediaSettingsProfile : Profile, IAutoMapperProfile
    {
        public MediaSettingsProfile()
        {
            CreateMap<MediaSettings, MediaSettingsModel>()
                .ForMember(dest => dest.PicturesStoredIntoDatabase, mo => mo.Ignore())
                .ForMember(dest => dest.ActiveStore, mo => mo.Ignore())
                .ForMember(dest => dest.UserFields, mo => mo.Ignore());
            CreateMap<MediaSettingsModel, MediaSettings>();
        }

        public int Order => 0;
    }
}