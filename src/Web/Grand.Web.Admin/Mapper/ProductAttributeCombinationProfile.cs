using AutoMapper;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Mapper
{
    public class ProductAttributeCombinationProfile : Profile, IAutoMapperProfile
    {
        public ProductAttributeCombinationProfile()
        {
            CreateMap<ProductAttributeCombination, ProductAttributeCombinationModel>()
                .ForMember(dest => dest.UseMultipleWarehouses, mo => mo.Ignore())
                .ForMember(dest => dest.PrimaryStoreCurrencyCode, mo => mo.Ignore())
                .ForMember(dest => dest.WarehouseInventoryModels, mo => mo.Ignore());
            CreateMap<ProductAttributeCombinationModel, ProductAttributeCombination>()
                .ForMember(dest => dest.WarehouseInventory, mo => mo.Ignore())
                .ForMember(dest => dest.Id, mo => mo.Ignore());

            CreateMap<PredefinedProductAttributeValue, ProductAttributeValue>()
               .ForMember(dest => dest.Id, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}