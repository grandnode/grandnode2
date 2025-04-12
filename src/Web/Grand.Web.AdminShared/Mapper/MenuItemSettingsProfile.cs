using AutoMapper;
using Grand.Domain.Common;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Settings;

namespace Grand.Web.AdminShared.Mapper;

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