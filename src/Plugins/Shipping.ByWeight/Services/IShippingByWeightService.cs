using Grand.Domain;
using Shipping.ByWeight.Domain;
using System.Threading.Tasks;

namespace Shipping.ByWeight.Services
{
    public partial interface IShippingByWeightService
    {
        Task DeleteShippingByWeightRecord(ShippingByWeightRecord shippingByWeightRecord);

        Task<IPagedList<ShippingByWeightRecord>> GetAll(int pageIndex = 0, int pageSize = int.MaxValue);

        Task<ShippingByWeightRecord> FindRecord(string shippingMethodId,
            string storeId, string warehouseId,
            string countryId, string stateProvinceId, string zip, double weight);

        Task<ShippingByWeightRecord> GetById(string shippingByWeightRecordId);

        Task InsertShippingByWeightRecord(ShippingByWeightRecord shippingByWeightRecord);

        Task UpdateShippingByWeightRecord(ShippingByWeightRecord shippingByWeightRecord);
    }

}
