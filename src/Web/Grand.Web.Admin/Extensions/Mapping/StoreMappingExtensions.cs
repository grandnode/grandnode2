using Grand.Infrastructure.Mapper;
using Grand.Domain.Stores;
using Grand.Web.Admin.Models.Stores;

namespace Grand.Web.Admin.Extensions
{
    public static class StoreMappingExtensions
    {
        public static StoreModel ToModel(this Store entity)
        {
            return entity.MapTo<Store, StoreModel>();
        }

        public static Store ToEntity(this StoreModel model)
        {
            return model.MapTo<StoreModel, Store>();
        }

        public static Store ToEntity(this StoreModel model, Store destination)
        {
            return model.MapTo(destination);
        }
    }
}