using AutoMapper;
using Grand.Domain.Customers;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Customers;

namespace Grand.Web.Admin.Mapper
{
    public class CustomerAttributeValueProfile : Profile, IAutoMapperProfile
    {
        public CustomerAttributeValueProfile()
        {
            CreateMap<CustomerAttributeValue, CustomerAttributeValueModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.Ignore());
            CreateMap<CustomerAttributeValueModel, CustomerAttributeValue>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()));
        }

        public int Order => 0;
    }
}