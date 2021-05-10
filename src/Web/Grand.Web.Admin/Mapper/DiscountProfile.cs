using AutoMapper;
using Grand.Domain.Discounts;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Discounts;
using System.Collections.Generic;
using System.Linq;

namespace Grand.Web.Admin.Mapper
{
    public class DiscountProfile : Profile, IAutoMapperProfile
    {
        public DiscountProfile()
        {
            CreateMap<Discount, DiscountModel>()
                .ForMember(dest => dest.DiscountTypeName, mo => mo.Ignore())
                .ForMember(dest => dest.TimesUsed, mo => mo.Ignore())
                .ForMember(dest => dest.AvailableCurrencies, mo => mo.Ignore())
                .ForMember(dest => dest.AddDiscountRequirement, mo => mo.Ignore())
                .ForMember(dest => dest.AvailableDiscountRequirementRules, mo => mo.Ignore())
                .ForMember(dest => dest.AvailableDiscountAmountProviders, mo => mo.Ignore())
                .ForMember(dest => dest.DiscountRequirementMetaInfos, mo => mo.Ignore());

            CreateMap<DiscountModel, Discount>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.LimitedToStores, mo => mo.MapFrom(x => x.Stores != null && x.Stores.Any()))
                .ForMember(dest => dest.DiscountRules, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}