using AutoMapper;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Shipping;

namespace Grand.Web.Admin.Mapper
{
    public class ShippingMethodProfile : Profile, IAutoMapperProfile
    {
        public ShippingMethodProfile()
        {
            CreateMap<ShippingMethod, ShippingMethodModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore());

            CreateMap<ShippingMethodModel, ShippingMethod>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()))
                .ForMember(dest => dest.RestrictedCountries, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}