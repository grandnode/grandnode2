using AutoMapper;
using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Shipping;

namespace Grand.Web.Admin.Mapper
{
    public class ShippingRateComputationMethodProfile : Profile, IAutoMapperProfile
    {
        public ShippingRateComputationMethodProfile()
        {
            CreateMap<IShippingRateCalculationProvider, ShippingRateComputationMethodModel>()
                .ForMember(dest => dest.FriendlyName, mo => mo.MapFrom(src => src.FriendlyName))
                .ForMember(dest => dest.SystemName, mo => mo.MapFrom(src => src.SystemName))
                .ForMember(dest => dest.DisplayOrder, mo => mo.MapFrom(src => src.Priority))
                .ForMember(dest => dest.IsActive, mo => mo.Ignore())
                .ForMember(dest => dest.LogoUrl, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}