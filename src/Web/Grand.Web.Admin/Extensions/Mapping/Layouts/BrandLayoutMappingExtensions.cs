using Grand.Infrastructure.Mapper;
using Grand.Domain.Catalog;
using Grand.Web.Admin.Models.Layouts;

namespace Grand.Web.Admin.Extensions
{
    public static class BrandLayoutMappingExtensions
    {
        public static BrandLayoutModel ToModel(this BrandLayout entity)
        {
            return entity.MapTo<BrandLayout, BrandLayoutModel>();
        }

        public static BrandLayout ToEntity(this BrandLayoutModel model)
        {
            return model.MapTo<BrandLayoutModel, BrandLayout>();
        }

        public static BrandLayout ToEntity(this BrandLayoutModel model, BrandLayout destination)
        {
            return model.MapTo(destination);
        }
    }
}