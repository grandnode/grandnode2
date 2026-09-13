using Grand.Domain;
using Shipping.ByWeight.Domain;

namespace Shipping.ByWeight.Services;

public interface IShippingByWeightService
{
    Task DeleteShippingByWeightRecord(ShippingByWeightRecord shippingByWeightRecord);

    /// <summary>
    ///     Gets shipping by weight records
    /// </summary>
    /// <param name="storeId">The store identifier; pass "" to load records of all stores</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    Task<IPagedList<ShippingByWeightRecord>> GetAll(string storeId = "", int pageIndex = 0,
        int pageSize = int.MaxValue);

    Task<ShippingByWeightRecord> FindRecord(string shippingMethodId,
        string storeId, string warehouseId,
        string countryId, string stateProvinceId, string zip, double weight);

    Task<ShippingByWeightRecord> GetById(string shippingByWeightRecordId);

    Task InsertShippingByWeightRecord(ShippingByWeightRecord shippingByWeightRecord);

    Task UpdateShippingByWeightRecord(ShippingByWeightRecord shippingByWeightRecord);
}