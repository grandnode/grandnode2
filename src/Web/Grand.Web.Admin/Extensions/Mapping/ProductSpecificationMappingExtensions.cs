using Grand.Infrastructure.Mapper;
using Grand.Domain.Catalog;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Extensions
{
    public static class ProductSpecificationMappingExtensions
    {
        public static ProductModel.AddProductSpecificationAttributeModel ToModel(this ProductSpecificationAttribute entity)
        {
            return entity.MapTo<ProductSpecificationAttribute, ProductModel.AddProductSpecificationAttributeModel>();
        }

        public static ProductSpecificationAttribute ToEntity(this ProductModel.AddProductSpecificationAttributeModel model)
        {
            return model.MapTo<ProductModel.AddProductSpecificationAttributeModel, ProductSpecificationAttribute>();
        }

        public static ProductSpecificationAttribute ToEntity(this ProductModel.AddProductSpecificationAttributeModel model, ProductSpecificationAttribute destination)
        {
            if(model.AttributeTypeId != SpecificationAttributeType.Option)
            {
                model.SpecificationAttributeId = "";
                model.SpecificationAttributeOptionId = "";
                model.AllowFiltering = false;
            }
            return model.MapTo(destination);
        }
    }
}