using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Catalog.Services.Products;

/// <summary>
///     Product tag service
/// </summary>
public class ProductTagService : IProductTagService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="productTagRepository">Product tag repository</param>
    /// <param name="productRepository">Product repository</param>
    /// <param name="cacheBase">Cache manager</param>
    /// <param name="mediator">Mediator</param>
    public ProductTagService(IRepository<ProductTag> productTagRepository,
        IRepository<Product> productRepository,
        ICacheBase cacheBase,
        IMediator mediator
    )
    {
        _productTagRepository = productTagRepository;
        _cacheBase = cacheBase;
        _mediator = mediator;
        _productRepository = productRepository;
    }

    #endregion

    #region Utilities

    /// <summary>
    ///     Get product count for each of existing product tag
    /// </summary>
    /// <returns>Dictionary of "product tag ID : product count"</returns>
    private async Task<Dictionary<string, int>> GetProductCount()
    {
        return await _cacheBase.GetAsync(CacheKey.PRODUCTTAG_COUNT_KEY, async () =>
        {
            var query = from pt in _productTagRepository.Table
                select pt;

            var dictionary = query.ToList().ToDictionary(item => item.Id, item => item.Count);
            return await Task.FromResult(dictionary);
        });
    }

    #endregion

    #region Fields

    private readonly IRepository<ProductTag> _productTagRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly ICacheBase _cacheBase;
    private readonly IMediator _mediator;

    #endregion

    #region Methods

    /// <summary>
    ///     Gets all product tags
    /// </summary>
    /// <returns>Product tags</returns>
    public virtual async Task<IList<ProductTag>> GetAllProductTags()
    {
        return await _cacheBase.GetAsync(CacheKey.PRODUCTTAG_ALL_KEY,
            async () => await Task.FromResult(_productTagRepository.Table.ToList()));
    }

    /// <summary>
    ///     Gets product tag
    /// </summary>
    /// <param name="productTagId">Product tag identifier</param>
    /// <returns>Product tag</returns>
    public virtual Task<ProductTag> GetProductTagById(string productTagId)
    {
        return _productTagRepository.GetByIdAsync(productTagId);
    }

    /// <summary>
    ///     Gets product tag by name
    /// </summary>
    /// <param name="name">Product tag name</param>
    /// <returns>Product tag</returns>
    public virtual Task<ProductTag> GetProductTagByName(string name)
    {
        return _productTagRepository.GetOneAsync(x => x.Name == name);
    }

    /// <summary>
    ///     Gets product tag by se-name
    /// </summary>
    /// <param name="sename">Product tag se-name</param>
    /// <returns>Product tag</returns>
    public virtual Task<ProductTag> GetProductTagBySeName(string sename)
    {
        return _productTagRepository.GetOneAsync(x => x.SeName == sename);
    }

    /// <summary>
    ///     Inserts a product tag
    /// </summary>
    /// <param name="productTag">Product tag</param>
    public virtual async Task InsertProductTag(ProductTag productTag)
    {
        ArgumentNullException.ThrowIfNull(productTag);

        await _productTagRepository.InsertAsync(productTag);

        //cache
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTTAG_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(productTag);
    }

    /// <summary>
    ///     Inserts a product tag
    /// </summary>
    /// <param name="productTag">Product tag</param>
    public virtual async Task UpdateProductTag(ProductTag productTag)
    {
        ArgumentNullException.ThrowIfNull(productTag);

        var previous = await GetProductTagById(productTag.Id);

        await _productTagRepository.UpdateAsync(productTag);

        //update on products
        await _productRepository.UpdateToSet(x => x.ProductTags, previous.Name, productTag.Name);

        //cache
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTTAG_PATTERN_KEY);

        //event notification
        await _mediator.EntityUpdated(productTag);
    }

    /// <summary>
    ///     Delete a product tag
    /// </summary>
    /// <param name="productTag">Product tag</param>
    public virtual async Task DeleteProductTag(ProductTag productTag)
    {
        ArgumentNullException.ThrowIfNull(productTag);

        //update product
        await _productRepository.Pull(string.Empty, x => x.ProductTags, productTag.Name);

        //delete tag
        await _productTagRepository.DeleteAsync(productTag);

        //cache
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTTAG_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);

        //event notification
        await _mediator.EntityDeleted(productTag);
    }

    /// <summary>
    ///     Attach a tag to the product
    /// </summary>
    /// <param name="productTag">Product tag</param>
    /// <param name="productId">Product ident</param>
    public virtual async Task AttachProductTag(ProductTag productTag, string productId)
    {
        ArgumentNullException.ThrowIfNull(productTag);

        //assign to product
        await _productRepository.AddToSet(productId, x => x.ProductTags, productTag.Name);

        //update product tag
        await _productTagRepository.UpdateField(productTag.Id, x => x.Count, productTag.Count + 1);

        //cache
        await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTTAG_PATTERN_KEY);

        //event notification
        await _mediator.EntityInserted(productTag);
    }

    /// <summary>
    ///     Detach a tag from the product
    /// </summary>
    /// <param name="productTag">Product Tag</param>
    /// <param name="productId">Product ident</param>
    public virtual async Task DetachProductTag(ProductTag productTag, string productId)
    {
        ArgumentNullException.ThrowIfNull(productTag);

        await _productRepository.Pull(productId, x => x.ProductTags, productTag.Name);

        //update product tag
        await _productTagRepository.UpdateField(productTag.Id, x => x.Count, productTag.Count - 1);

        //cache
        await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTTAG_PATTERN_KEY);

        //event notification
        await _mediator.EntityDeleted(productTag);
    }


    /// <summary>
    ///     Get number of products
    /// </summary>
    /// <param name="productTagId">Product tag identifier</param>
    /// <returns>Number of products</returns>
    public virtual async Task<int> GetProductCount(string productTagId)
    {
        var dictionary = await GetProductCount();
        return dictionary.TryGetValue(productTagId, out var value) ? value : 0;
    }

    #endregion
}