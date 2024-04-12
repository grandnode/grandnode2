using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Catalog.Services.Brands;

/// <summary>
///     Brand service
/// </summary>
public class BrandService : IBrandService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    public BrandService(ICacheBase cacheBase,
        IRepository<Brand> brandRepository,
        IWorkContext workContext,
        IMediator mediator, AccessControlConfig accessControlConfig)
    {
        _cacheBase = cacheBase;
        _brandRepository = brandRepository;
        _workContext = workContext;
        _mediator = mediator;
        _accessControlConfig = accessControlConfig;
    }

    #endregion

    #region Fields

    private readonly IRepository<Brand> _brandRepository;
    private readonly IWorkContext _workContext;
    private readonly IMediator _mediator;
    private readonly ICacheBase _cacheBase;
    private readonly AccessControlConfig _accessControlConfig;

    #endregion

    #region Methods

    /// <summary>
    ///     Gets all brands
    /// </summary>
    /// <param name="brandName">Brand name</param>
    /// <param name="storeId">Store ident</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
    /// <returns>Brands</returns>
    public virtual async Task<IPagedList<Brand>> GetAllBrands(string brandName = "",
        string storeId = "",
        int pageIndex = 0,
        int pageSize = int.MaxValue,
        bool showHidden = false)
    {
        var query = from m in _brandRepository.Table
            select m;

        if (!showHidden)
            query = query.Where(m => m.Published);
        if (!string.IsNullOrWhiteSpace(brandName))
            query = query.Where(m => m.Name != null && m.Name.ToLower().Contains(brandName.ToLower()));

        if (!_accessControlConfig.IgnoreAcl ||
            (!string.IsNullOrEmpty(storeId) && !_accessControlConfig.IgnoreStoreLimitations))
        {
            if (!showHidden && !_accessControlConfig.IgnoreAcl)
            {
                //Limited to customer groups rules
                var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                query = from p in query
                    where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                    select p;
            }

            if (!string.IsNullOrEmpty(storeId) && !_accessControlConfig.IgnoreStoreLimitations)
                //Limited to stores rules
                query = from p in query
                    where !p.LimitedToStores || p.Stores.Contains(storeId)
                    select p;
        }

        query = query.OrderBy(m => m.DisplayOrder).ThenBy(m => m.Name);
        return await PagedList<Brand>.Create(query, pageIndex, pageSize);
    }

    /// <summary>
    ///     Gets a brand
    /// </summary>
    /// <param name="brandId">Brand id</param>
    /// <returns>Brand</returns>
    public virtual Task<Brand> GetBrandById(string brandId)
    {
        var key = string.Format(CacheKey.BRANDS_BY_ID_KEY, brandId);
        return _cacheBase.GetAsync(key, () => _brandRepository.GetByIdAsync(brandId));
    }

    /// <summary>
    ///     Inserts a brand
    /// </summary>
    /// <param name="brand">Brand</param>
    public virtual async Task InsertBrand(Brand brand)
    {
        ArgumentNullException.ThrowIfNull(brand);

        await _brandRepository.InsertAsync(brand);

        //cache
        await _cacheBase.RemoveByPrefix(CacheKey.BRANDS_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(brand);
    }

    /// <summary>
    ///     Updates the brand
    /// </summary>
    /// <param name="brand">Brand</param>
    public virtual async Task UpdateBrand(Brand brand)
    {
        ArgumentNullException.ThrowIfNull(brand);

        await _brandRepository.UpdateAsync(brand);

        //cache
        await _cacheBase.RemoveByPrefix(CacheKey.BRANDS_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(brand);
    }

    /// <summary>
    ///     Deletes a brand
    /// </summary>
    /// <param name="brand">Brand</param>
    public virtual async Task DeleteBrand(Brand brand)
    {
        ArgumentNullException.ThrowIfNull(brand);

        await _cacheBase.RemoveByPrefix(CacheKey.BRANDS_PATTERN_KEY);

        await _brandRepository.DeleteAsync(brand);

        //event notification
        await _mediator.EntityDeleted(brand);
    }

    /// <summary>
    ///     Gets a discount brand mapping
    /// </summary>
    /// <param name="discountId">Discount id mapping id</param>
    /// <returns>Product brand mapping</returns>
    public virtual async Task<IList<Brand>> GetAllBrandsByDiscount(string discountId)
    {
        var query = from c in _brandRepository.Table
            where c.AppliedDiscounts.Any(x => x == discountId)
            select c;

        return await Task.FromResult(query.ToList());
    }

    #endregion
}