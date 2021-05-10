using Grand.Api.DTOs.Catalog;
using Grand.Api.DTOs.Common;
using Grand.Api.DTOs.Customers;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Infrastructure.Mapper;

namespace Grand.Api.Extensions
{
    public static class MappingExtensions
    {

        #region Product

        public static ProductDto ToModel(this Product entity)
        {
            return entity.MapTo<Product, ProductDto>();
        }

        public static Product ToEntity(this ProductDto model)
        {
            return model.MapTo<ProductDto, Product>();
        }

        public static Product ToEntity(this ProductDto model, Product destination)
        {
            return model.MapTo(destination);
        }

        #endregion

        #region Category

        public static CategoryDto ToModel(this Category entity)
        {
            return entity.MapTo<Category, CategoryDto>();
        }

        public static Category ToEntity(this CategoryDto model)
        {
            return model.MapTo<CategoryDto, Category>();
        }

        public static Category ToEntity(this CategoryDto model, Category destination)
        {
            return model.MapTo(destination);
        }

        #endregion

        #region Brand

        public static BrandDto ToModel(this Brand entity)
        {
            return entity.MapTo<Brand, BrandDto>();
        }

        public static Brand ToEntity(this BrandDto model)
        {
            return model.MapTo<BrandDto, Brand>();
        }

        public static Brand ToEntity(this BrandDto model, Brand destination)
        {
            return model.MapTo(destination);
        }

        #endregion

        #region Collection
        public static CollectionDto ToModel(this Collection entity)
        {
            return entity.MapTo<Collection, CollectionDto>();
        }

        public static Collection ToEntity(this CollectionDto model)
        {
            return model.MapTo<CollectionDto, Collection>();
        }

        public static Collection ToEntity(this CollectionDto model, Collection destination)
        {
            return model.MapTo(destination);
        }

        #endregion

        #region Product attribute
        public static ProductAttributeDto ToModel(this ProductAttribute entity)
        {
            return entity.MapTo<ProductAttribute, ProductAttributeDto>();
        }

        public static ProductAttribute ToEntity(this ProductAttributeDto model)
        {
            return model.MapTo<ProductAttributeDto, ProductAttribute>();
        }

        public static ProductAttribute ToEntity(this ProductAttributeDto model, ProductAttribute destination)
        {
            return model.MapTo(destination);
        }

        #endregion

        #region Product attribute mapping

        public static ProductAttributeMappingDto ToModel(this ProductAttributeMapping entity)
        {
            return entity.MapTo<ProductAttributeMapping, ProductAttributeMappingDto>();
        }

        public static ProductAttributeMapping ToEntity(this ProductAttributeMappingDto model)
        {
            return model.MapTo<ProductAttributeMappingDto, ProductAttributeMapping>();
        }

        public static ProductAttributeMapping ToEntity(this ProductAttributeMappingDto model, ProductAttributeMapping destination)
        {
            return model.MapTo(destination);
        }

        #endregion

        #region Specification attribute
        public static SpecificationAttributeDto ToModel(this SpecificationAttribute entity)
        {
            return entity.MapTo<SpecificationAttribute, SpecificationAttributeDto>();
        }

        public static SpecificationAttribute ToEntity(this SpecificationAttributeDto model)
        {
            return model.MapTo<SpecificationAttributeDto, SpecificationAttribute>();
        }

        public static SpecificationAttribute ToEntity(this SpecificationAttributeDto model, SpecificationAttribute destination)
        {
            return model.MapTo(destination);
        }

        #endregion

        #region Tier prices

        public static ProductTierPriceDto ToModel(this TierPrice entity)
        {
            return entity.MapTo<TierPrice, ProductTierPriceDto>();
        }

        public static TierPrice ToEntity(this ProductTierPriceDto model)
        {
            return model.MapTo<ProductTierPriceDto, TierPrice>();
        }

        public static TierPrice ToEntity(this ProductTierPriceDto model, TierPrice destination)
        {
            return model.MapTo(destination);
        }

        #endregion

        #region Customer group
        public static CustomerGroupDto ToModel(this CustomerGroup entity)
        {
            return entity.MapTo<CustomerGroup, CustomerGroupDto>();
        }

        public static CustomerGroup ToEntity(this CustomerGroupDto model)
        {
            return model.MapTo<CustomerGroupDto, CustomerGroup>();
        }

        public static CustomerGroup ToEntity(this CustomerGroupDto model, CustomerGroup destination)
        {
            return model.MapTo(destination);
        }

        #endregion

        #region Customer

        public static CustomerDto ToModel(this Customer entity)
        {
            return entity.MapTo<Customer, CustomerDto>();
        }

        public static Customer ToEntity(this CustomerDto model)
        {
            return model.MapTo<CustomerDto, Customer>();
        }

        public static Customer ToEntity(this CustomerDto model, Customer destination)
        {
            return model.MapTo(destination);
        }

        #endregion

        #region Customer address
        public static AddressDto ToModel(this Address entity)
        {
            return entity.MapTo<Address, AddressDto>();
        }

        public static Address ToEntity(this AddressDto model)
        {
            return model.MapTo<AddressDto, Address>();
        }
        public static Address ToEntity(this AddressDto model, Address destination)
        {
            return model.MapTo(destination);
        }


        #endregion

        #region Picture

        public static PictureDto ToModel(this Picture entity)
        {
            return entity.MapTo<Picture, PictureDto>();
        }

        #endregion
    }
}
