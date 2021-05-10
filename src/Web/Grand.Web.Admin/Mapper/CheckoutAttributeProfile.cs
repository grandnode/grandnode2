using AutoMapper;
using Grand.Domain.Orders;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Orders;
using System.Linq;

namespace Grand.Web.Admin.Mapper
{
    public class CheckoutAttributeProfile : Profile, IAutoMapperProfile
    {
        public CheckoutAttributeProfile()
        {
            CreateMap<CheckoutAttribute, CheckoutAttributeModel>()
                .ForMember(dest => dest.AvailableTaxCategories, mo => mo.Ignore())
                .ForMember(dest => dest.AttributeControlTypeName, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.ConditionAllowed, mo => mo.Ignore())
                .ForMember(dest => dest.ConditionModel, mo => mo.Ignore());
            CreateMap<CheckoutAttributeModel, CheckoutAttribute>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()))
                .ForMember(dest => dest.ConditionAttribute, mo => mo.Ignore())
                .ForMember(dest => dest.LimitedToGroups, mo => mo.MapFrom(x => x.CustomerGroups != null && x.CustomerGroups.Any()))
                .ForMember(dest => dest.LimitedToStores, mo => mo.MapFrom(x => x.Stores != null && x.Stores.Any()))
                .ForMember(dest => dest.CheckoutAttributeValues, mo => mo.Ignore());

            CreateMap<CheckoutAttributeValue, CheckoutAttributeValueModel>()
               .ForMember(dest => dest.Locales, mo => mo.Ignore());
            CreateMap<CheckoutAttributeValueModel, CheckoutAttributeValue>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()));
        }

        public int Order => 0;
    }
}