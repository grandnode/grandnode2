using AutoMapper;
using Grand.Domain.Admin;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Menu;

namespace Grand.Web.AdminShared.Mapper;

public class MenuProfile : Profile, IAutoMapperProfile
{
    public MenuProfile()
    {
        CreateMap<AdminSiteMap, MenuModel>()
            .ForMember(dest => dest.UserFields, mo => mo.Ignore());
        CreateMap<MenuModel, AdminSiteMap>()
            .ForMember(dest => dest.ChildNodes, mo => mo.Ignore());
    }

    public int Order => 0;
}