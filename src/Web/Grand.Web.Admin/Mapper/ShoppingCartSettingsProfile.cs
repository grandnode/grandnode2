using AutoMapper;
using Grand.Domain.Orders;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Mapper;

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