using AutoMapper;
using Grand.Domain.Customers;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Customers;

namespace Grand.Web.Admin.Mapper
{
    public class CustomerAttributeProfile : Profile, IAutoMapperProfile
    {
        public CustomerAttributeProfile()
        {
            CreateMap<CustomerAttribute, CustomerAttributeModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.AttributeControlTypeName, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.Ignore());
            CreateMap<CustomerAttributeModel, CustomerAttribute>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.MapFrom(x => x.Locales.ToTranslationProperty()))
                .ForMember(dest => dest.CustomerAttributeValues, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}