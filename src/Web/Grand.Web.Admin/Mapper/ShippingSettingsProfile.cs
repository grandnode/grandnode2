using AutoMapper;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Shipping;

namespace Grand.Web.Admin.Mapper
{
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
}