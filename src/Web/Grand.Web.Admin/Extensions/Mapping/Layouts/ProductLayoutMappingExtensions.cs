using Grand.Infrastructure.Mapper;
using Grand.Domain.Catalog;
using Grand.Web.Admin.Models.Layouts;

namespace Grand.Web.Admin.Extensions
{
    public static class ProductLayoutMappingExtensions
    {
        public static ProductLayoutModel ToModel(this ProductLayout entity)
        {
            return entity.MapTo<ProductLayout, ProductLayoutModel>();
        }

        public static ProductLayout ToEntity(this ProductLayoutModel model)
        {
            return model.MapTo<ProductLayoutModel, ProductLayout>();
        }

        public static ProductLayout ToEntity(this ProductLayoutModel model, ProductLayout destination)
        {
            return model.MapTo(destination);
        }
    }
}