using AutoMapper;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Mapper
{
    public class ProductAttributeMappingProfile : Profile, IAutoMapperProfile
    {
        public ProductAttributeMappingProfile()
        {
            CreateMap<ProductAttributeMapping, ProductModel.ProductAttributeMappingModel>();

            CreateMap<ProductModel.ProductAttributeMappingModel, ProductAttributeMapping>()
                .ForMember(dest => dest.Id, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}