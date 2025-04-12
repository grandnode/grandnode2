using AutoMapper;
using Grand.Domain.Orders;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Settings;

namespace Grand.Web.AdminShared.Mapper;

public class ShoppingCartSettingsProfile : Profile, IAutoMapperProfile
{
    public ShoppingCartSettingsProfile()
    {
        CreateMap<ShoppingCartSettings, SalesSettingsModel.ShoppingCartSettingsModel>()
            .ForMember(dest => dest.UserFields, mo => mo.Ignore());

        CreateMap<SalesSettingsModel.ShoppingCartSettingsModel, ShoppingCartSettings>();
    }

    public int Order => 0;
}