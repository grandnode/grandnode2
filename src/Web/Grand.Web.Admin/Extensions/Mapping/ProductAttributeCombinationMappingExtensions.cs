using Grand.Infrastructure.Mapper;
using Grand.Domain.Catalog;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Extensions
{
    public static class ProductAttributeCombinationMappingExtensions
    {
        public static ProductAttributeCombinationModel ToModel(this ProductAttributeCombination entity)
        {
            return entity.MapTo<ProductAttributeCombination, ProductAttributeCombinationModel>();
        }

        public static ProductAttributeCombination ToEntity(this ProductAttributeCombinationModel model)
        {
            return model.MapTo<ProductAttributeCombinationModel, ProductAttributeCombination>();
        }

        public static ProductAttributeCombination ToEntity(this ProductAttributeCombinationModel model, ProductAttributeCombination destination)
        {
            return model.MapTo(destination);
        }
    }
}