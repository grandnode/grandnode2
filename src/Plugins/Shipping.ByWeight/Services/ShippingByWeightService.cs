using Grand.Data;
using Grand.Domain;
using Grand.Infrastructure.Caching;
using Shipping.ByWeight.Domain;

namespace Shipping.ByWeight.Services;

public class ShippingByWeightService : IShippingByWeightService
{
    #region Ctor

    public ShippingByWeightService(ICacheBase cacheBase,
        IRepository<ShippingByWeightRecord> sbwRepository)
    {
        _cacheBase = cacheBase;
        _sbwRepository = sbwRepository;
    }

    #endregion

    #region Constants

    private const string SHIPPINGBYWEIGHT_ALL_KEY = "Grand.shippingbyweight.all-{0}-{1}";
    private const string SHIPPINGBYWEIGHT_PATTERN_KEY = "Grand.shippingbyweight.";

    #endregion

    #region Fields

    private readonly IRepository<ShippingByWeightRecord> _sbwRepository;
    private readonly ICacheBase _cacheBase;

    #endregion

    #region Methods

    public virtual async Task DeleteShippingByWeightRecord(ShippingByWeightRecord shippingByWeightRecord)
    {
        ArgumentNullException.ThrowIfNull(shippingByWeightRecord);

        await _sbwRepository.DeleteAsync(shippingByWeightRecord);

        await _cacheBase.RemoveByPrefix(SHIPPINGBYWEIGHT_PATTERN_KEY);
    }

    public virtual async Task<IPagedList<ShippingByWeightRecord>> GetAll(int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var key = string.Format(SHIPPINGBYWEIGHT_ALL_KEY, pageIndex, pageSize);
        return await _cacheBase.GetAsync(key, () =>
        {
            var query = from sbw in _sbwRepository.Table
                select sbw;

            return Task.FromResult(new PagedList<ShippingByWeightRecord>(query, pageIndex, pageSize));
        });
    }

    public virtual async Task<ShippingByWeightRecord> FindRecord(string shippingMethodId,
        string storeId, string warehouseId,
        string countryId, string stateProvinceId, string zip, double weight)
    {
        zip ??= string.Empty;
        zip = zip.Trim();

        var existingRates = (await GetAll())
            .Where(sbw => sbw.ShippingMethodId == shippingMethodId && weight >= sbw.From && weight <= sbw.To)
            .ToList();

        if (!string.IsNullOrEmpty(warehouseId))
            existingRates = existingRates.Any(x => x.WarehouseId == warehouseId)
                ? existingRates.Where(x => x.WarehouseId == warehouseId).ToList()
                : existingRates.Where(x => string.IsNullOrEmpty(x.WarehouseId)).ToList();

        if (!string.IsNullOrEmpty(storeId))
            existingRates = existingRates.Any(x => x.StoreId == storeId)
                ? existingRates.Where(x => x.StoreId == storeId).ToList()
                : existingRates.Where(x => string.IsNullOrEmpty(x.StoreId)).ToList();

        if (!string.IsNullOrEmpty(countryId))
            existingRates = existingRates.Any(x => x.CountryId == countryId)
                ? existingRates.Where(x => x.CountryId == countryId).ToList()
                : existingRates.Where(x => string.IsNullOrEmpty(x.CountryId)).ToList();

        if (!string.IsNullOrEmpty(stateProvinceId))
            existingRates = existingRates.Any(x => x.StateProvinceId == stateProvinceId)
                ? existingRates.Where(x => x.StateProvinceId == stateProvinceId).ToList()
                : existingRates.Where(x => string.IsNullOrEmpty(x.StateProvinceId)).ToList();

        if (!string.IsNullOrEmpty(zip))
            existingRates = existingRates.Any(x => x.Zip == zip)
                ? existingRates.Where(x => x.Zip == zip).ToList()
                : existingRates.Where(x => string.IsNullOrEmpty(x.Zip)).ToList();

        return existingRates.FirstOrDefault();
    }

    public virtual Task<ShippingByWeightRecord> GetById(string shippingByWeightRecordId)
    {
        return _sbwRepository.GetByIdAsync(shippingByWeightRecordId);
    }

    public virtual async Task InsertShippingByWeightRecord(ShippingByWeightRecord shippingByWeightRecord)
    {
        ArgumentNullException.ThrowIfNull(shippingByWeightRecord);

        await _sbwRepository.InsertAsync(shippingByWeightRecord);

        await _cacheBase.RemoveByPrefix(SHIPPINGBYWEIGHT_PATTERN_KEY);
    }

    public virtual async Task UpdateShippingByWeightRecord(ShippingByWeightRecord shippingByWeightRecord)
    {
        ArgumentNullException.ThrowIfNull(shippingByWeightRecord);

        await _sbwRepository.UpdateAsync(shippingByWeightRecord);

        await _cacheBase.RemoveByPrefix(SHIPPINGBYWEIGHT_PATTERN_KEY);
    }

    #endregion
}