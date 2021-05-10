using Grand.Infrastructure.Mapper;
using Grand.Domain.Catalog;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Extensions
{
    public static class BrandMappingExtensions
    {
        public static BrandModel ToModel(this Brand entity)
        {
            return entity.MapTo<Brand, BrandModel>();
        }

        public static Brand ToEntity(this BrandModel model)
        {
            return model.MapTo<BrandModel, Brand>();
        }

        public static Brand ToEntity(this BrandModel model, Brand destination)
        {
            return model.MapTo(destination);
        }
    }
}