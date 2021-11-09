using AutoMapper;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;
using Grand.Web.Common.Themes;

namespace Grand.Web.Admin.Mapper
{
    public class ThemeConfigurationProfile : Profile, IAutoMapperProfile
    {
        public ThemeConfigurationProfile()
        {
            CreateMap<ThemeConfiguration, GeneralCommonSettingsModel.StoreInformationSettingsModel.ThemeConfigurationModel>()
                 .ForMember(dest => dest.ThemeTitle, mo => mo.MapFrom(p => p.Title))
                 .ForMember(dest => dest.ThemeName, mo => mo.MapFrom(p => p.Name))
                 .ForMember(dest => dest.ThemeVersion, mo => mo.MapFrom(p => p.Version));
        }

        public int Order => 0;
    }
}