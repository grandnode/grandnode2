using Grand.Domain.Catalog;
using Grand.Web.Admin.Models.Catalog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Interfaces
{
    public interface ICollectionViewModelService
    {
        void PrepareSortOptionsModel(CollectionModel model);
        Task PrepareLayoutsModel(CollectionModel model);
        Task PrepareDiscountModel(CollectionModel model, Collection collection, bool excludeProperties);
        Task<Collection> InsertCollectionModel(CollectionModel model);
        Task<Collection> UpdateCollectionModel(Collection collection, CollectionModel model);
        Task DeleteCollection(Collection collection);
        Task<CollectionModel.AddCollectionProductModel> PrepareAddCollectionProductModel(string storeId);
        Task<(IList<ProductModel> products, int totalCount)> PrepareProductModel(CollectionModel.AddCollectionProductModel model, int pageIndex, int pageSize);
        Task<(IEnumerable<CollectionModel.CollectionProductModel> collectionProductModels, int totalCount)> PrepareCollectionProductModel(string collectionId, string storeId, int pageIndex, int pageSize);
        Task ProductUpdate(CollectionModel.CollectionProductModel model);
        Task ProductDelete(string id, string productId);
        Task InsertCollectionProductModel(CollectionModel.AddCollectionProductModel model);
        Task<(IEnumerable<CollectionModel.ActivityLogModel> activityLogModels, int totalCount)> PrepareActivityLogModel(string collectionId, int pageIndex, int pageSize);
    }
}
