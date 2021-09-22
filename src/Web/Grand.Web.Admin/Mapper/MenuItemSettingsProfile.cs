using AutoMapper;
using Grand.Domain.Common;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Mapper
{
    public class MenuItemSettingsProfile : Profile, IAutoMapperProfile
    {
        public MenuItemSettingsProfile()
        {
            CreateMap<MenuItemSettings, GeneralCommonSettingsModel.DisplayMenuSettingsModel>()
                .ForMember(dest => dest.UserFields, mo => mo.Ignore());
            CreateMap<GeneralCommonSettingsModel.DisplayMenuSettingsModel, MenuItemSettings>();
        }

        public int Order => 0;
    }
}