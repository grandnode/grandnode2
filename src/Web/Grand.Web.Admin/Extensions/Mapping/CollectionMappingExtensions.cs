using Grand.Infrastructure.Mapper;
using Grand.Domain.Catalog;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Extensions
{
    public static class CollectionMappingExtensions
    {
        public static CollectionModel ToModel(this Collection entity)
        {
            return entity.MapTo<Collection, CollectionModel>();
        }

        public static Collection ToEntity(this CollectionModel model)
        {
            return model.MapTo<CollectionModel, Collection>();
        }

        public static Collection ToEntity(this CollectionModel model, Collection destination)
        {
            return model.MapTo(destination);
        }
    }
}