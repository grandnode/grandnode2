using Grand.Domain.Stores;
using Grand.Web.Admin.Models.Stores;

namespace Grand.Web.Admin.Interfaces
{
    public interface IStoreViewModelService
    {
        Task PrepareLanguagesModel(StoreModel model);
        Task PrepareWarehouseModel(StoreModel model);
        Task PrepareCountryModel(StoreModel model);
        Task PrepareCurrencyModel(StoreModel model);
        StoreModel PrepareStoreModel();
        Task<Store> InsertStoreModel(StoreModel model);
        Task<Store> UpdateStoreModel(Store store, StoreModel model);
        Task DeleteStore(Store store);
        Task InsertDomainHostModel(Store store, DomainHostModel model);
        Task UpdateDomainHostModel(Store store, DomainHostModel model);
        Task DeleteDomainHostModel(Store store, string id);

    }
}
