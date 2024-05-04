using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Catalog.Services.Products;

/// <summary>
///     Product layout service
/// </summary>
public class ProductLayoutService : IProductLayoutService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="productLayoutRepository">Product layout repository</param>
    /// <param name="cacheBase">Cache base</param>
    /// <param name="mediator">Mediator</param>
    public ProductLayoutService(IRepository<ProductLayout> productLayoutRepository,
        ICacheBase cacheBase,
        IMediator mediator)
    {
        _productLayoutRepository = productLayoutRepository;
        _cacheBase = cacheBase;
        _mediator = mediator;
    }

    #endregion

    #region Fields

    private readonly IRepository<ProductLayout> _productLayoutRepository;
    private readonly ICacheBase _cacheBase;
    private readonly IMediator _mediator;

    #endregion

    #region Methods

    /// <summary>
    ///     Gets all product layouts
    /// </summary>
    /// <returns>Product layouts</returns>
    public virtual async Task<IList<ProductLayout>> GetAllProductLayouts()
    {
        return await _cacheBase.GetAsync(CacheKey.PRODUCT_LAYOUT_ALL, async () =>
        {
            var query = from pt in _productLayoutRepository.Table
                orderby pt.DisplayOrder
                select pt;
            return await Task.FromResult(query.ToList());
        });
    }

    /// <summary>
    ///     Gets a product layout
    /// </summary>
    /// <param name="productLayoutId">Product layout identifier</param>
    /// <returns>Product layout</returns>
    public virtual Task<ProductLayout> GetProductLayoutById(string productLayoutId)
    {
        var key = string.Format(CacheKey.PRODUCT_LAYOUT_BY_ID_KEY, productLayoutId);
        return _cacheBase.GetAsync(key, () => _productLayoutRepository.GetByIdAsync(productLayoutId));
    }

    /// <summary>
    ///     Inserts product layout
    /// </summary>
    /// <param name="productLayout">Product layout</param>
    public virtual async Task InsertProductLayout(ProductLayout productLayout)
    {
        ArgumentNullException.ThrowIfNull(productLayout);

        await _productLayoutRepository.InsertAsync(productLayout);

        //clear cache
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCT_LAYOUT_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(productLayout);
    }

    /// <summary>
    ///     Updates the product layout
    /// </summary>
    /// <param name="productLayout">Product layout</param>
    public virtual async Task UpdateProductLayout(ProductLayout productLayout)
    {
        ArgumentNullException.ThrowIfNull(productLayout);

        await _productLayoutRepository.UpdateAsync(productLayout);

        //clear cache
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCT_LAYOUT_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(productLayout);
    }

    /// <summary>
    ///     Delete product layout
    /// </summary>
    /// <param name="productLayout">Product layout</param>
    public virtual async Task DeleteProductLayout(ProductLayout productLayout)
    {
        ArgumentNullException.ThrowIfNull(productLayout);

        await _productLayoutRepository.DeleteAsync(productLayout);

        //clear cache
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCT_LAYOUT_PATTERN_KEY);

        //event notification
        await _mediator.EntityDeleted(productLayout);
    }

    #endregion
}