using AutoMapper;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Mapper
{
    public class ProductSpecificationProfile : Profile, IAutoMapperProfile
    {
        public ProductSpecificationProfile()
        {
            CreateMap<ProductSpecificationAttribute, ProductModel.AddProductSpecificationAttributeModel>()
                .ForMember(dest => dest.AvailableAttributes, mo => mo.Ignore())
                .ForMember(dest => dest.AvailableOptions, mo => mo.Ignore());
            CreateMap<ProductModel.AddProductSpecificationAttributeModel, ProductSpecificationAttribute>();

        }

        public int Order => 0;
    }
}