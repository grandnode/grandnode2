using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Data;
using Grand.Domain.Tax;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Catalog.Services.Tax;

/// <summary>
///     Tax category service
/// </summary>
public class TaxCategoryService : ITaxCategoryService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="cacheBase">Cache manager</param>
    /// <param name="taxCategoryRepository">Tax category repository</param>
    /// <param name="mediator">Mediator</param>
    public TaxCategoryService(ICacheBase cacheBase,
        IRepository<TaxCategory> taxCategoryRepository,
        IMediator mediator)
    {
        _cacheBase = cacheBase;
        _taxCategoryRepository = taxCategoryRepository;
        _mediator = mediator;
    }

    #endregion

    #region Fields

    private readonly IRepository<TaxCategory> _taxCategoryRepository;
    private readonly IMediator _mediator;
    private readonly ICacheBase _cacheBase;

    #endregion

    #region Methods

    /// <summary>
    ///     Gets all tax categories
    /// </summary>
    /// <param name="storeId">Store identifier; pass empty to return all</param>
    /// <returns>Tax categories</returns>
    public virtual async Task<IList<TaxCategory>> GetAllTaxCategories(string storeId = "")
    {
        var key = string.Format(CacheKey.TAXCATEGORIES_ALL_KEY, storeId);
        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = _taxCategoryRepository.Table.AsQueryable();
            if (!string.IsNullOrEmpty(storeId))
                query = query.Where(tc => tc.StoreId == storeId || string.IsNullOrEmpty(tc.StoreId));
            return await Task.FromResult(query.OrderBy(tc => tc.DisplayOrder).ToList());
        });
    }

    /// <summary>
    ///     Gets a tax category
    /// </summary>
    /// <param name="taxCategoryId">Tax category identifier</param>
    /// <returns>Tax category</returns>
    public virtual Task<TaxCategory> GetTaxCategoryById(string taxCategoryId)
    {
        var key = string.Format(CacheKey.TAXCATEGORIES_BY_ID_KEY, taxCategoryId);
        return _cacheBase.GetAsync(key, () => _taxCategoryRepository.GetByIdAsync(taxCategoryId));
    }

    /// <summary>
    ///     Inserts a tax category
    /// </summary>
    /// <param name="taxCategory">Tax category</param>
    public virtual async Task InsertTaxCategory(TaxCategory taxCategory)
    {
        ArgumentNullException.ThrowIfNull(taxCategory);

        await _taxCategoryRepository.InsertAsync(taxCategory);

        await _cacheBase.RemoveByPrefix(CacheKey.TAXCATEGORIES_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(taxCategory);
    }

    /// <summary>
    ///     Updates the tax category
    /// </summary>
    /// <param name="taxCategory">Tax category</param>
    public virtual async Task UpdateTaxCategory(TaxCategory taxCategory)
    {
        ArgumentNullException.ThrowIfNull(taxCategory);

        await _taxCategoryRepository.UpdateAsync(taxCategory);

        await _cacheBase.RemoveByPrefix(CacheKey.TAXCATEGORIES_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(taxCategory);
    }

    /// <summary>
    ///     Deletes a tax category
    /// </summary>
    /// <param name="taxCategory">Tax category</param>
    public virtual async Task DeleteTaxCategory(TaxCategory taxCategory)
    {
        ArgumentNullException.ThrowIfNull(taxCategory);

        await _taxCategoryRepository.DeleteAsync(taxCategory);

        //clear tax categories cache
        await _cacheBase.RemoveByPrefix(CacheKey.TAXCATEGORIES_PATTERN_KEY);

        //clear product cache
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);

        //event notification
        await _mediator.EntityDeleted(taxCategory);
    }

    #endregion
}