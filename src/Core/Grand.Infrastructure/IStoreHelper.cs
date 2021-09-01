using Grand.Domain.Stores;
using System.Threading.Tasks;

namespace Grand.Infrastructure
{
    public interface IStoreHelper
    {
        Store StoreHost { get; }
        DomainHost DomainHost { get; }

        Task SetStoreCookie(string storeId);
        Task<Store> SetCurrentStore(string storeId);
    }
}
