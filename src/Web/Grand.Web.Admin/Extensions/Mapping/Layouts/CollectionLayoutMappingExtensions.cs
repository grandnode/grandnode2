using Grand.Infrastructure.Mapper;
using Grand.Domain.Catalog;
using Grand.Web.Admin.Models.Layouts;

namespace Grand.Web.Admin.Extensions
{
    public static class CollectionLayoutMappingExtensions
    {
        public static CollectionLayoutModel ToModel(this CollectionLayout entity)
        {
            return entity.MapTo<CollectionLayout, CollectionLayoutModel>();
        }

        public static CollectionLayout ToEntity(this CollectionLayoutModel model)
        {
            return model.MapTo<CollectionLayoutModel, CollectionLayout>();
        }

        public static CollectionLayout ToEntity(this CollectionLayoutModel model, CollectionLayout destination)
        {
            return model.MapTo(destination);
        }
    }
}