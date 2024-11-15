using AutoMapper;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;

namespace Grand.Module.Api.Infrastructure.Mapper.Profiles;

public class ProductAttributeProfile : Profile, IAutoMapperProfile
{
    public ProductAttributeProfile()
    {
        CreateMap<ProductAttributeDto, ProductAttribute>()
            .ForMember(dest => dest.UserFields, mo => mo.Ignore());

        CreateMap<ProductAttribute, ProductAttributeDto>();

        CreateMap<PredefinedProductAttributeValue, PredefinedProductAttributeValueDto>();

        CreateMap<PredefinedProductAttributeValueDto, PredefinedProductAttributeValue>()
            .ForMember(dest => dest.Locales, mo => mo.Ignore());
    }

    public int Order => 1;
}