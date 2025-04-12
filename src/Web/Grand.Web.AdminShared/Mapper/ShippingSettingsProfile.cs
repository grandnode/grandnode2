using AutoMapper;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Shipping;

namespace Grand.Web.AdminShared.Mapper;

public class ShippingSettingsProfile : Profile, IAutoMapperProfile
{
    public ShippingSettingsProfile()
    {
        CreateMap<ShippingSettings, ShippingSettingsModel>()
            .ForMember(dest => dest.ActiveStore, mo => mo.Ignore())
            .ForMember(dest => dest.UserFields, mo => mo.Ignore());

        CreateMap<ShippingSettingsModel, ShippingSettings>();
    }

    public int Order => 0;
}