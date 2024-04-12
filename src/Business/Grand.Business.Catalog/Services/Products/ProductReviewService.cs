using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Catalog.Services.Products;

/// <summary>
///     Product review service
/// </summary>
public class ProductReviewService : IProductReviewService
{
    #region Ctor

    public ProductReviewService(IRepository<ProductReview> productReviewRepository, IMediator mediator)
    {
        _productReviewRepository = productReviewRepository;
        _mediator = mediator;
    }

    #endregion

    /// <summary>
    ///     Gets all product reviews
    /// </summary>
    /// <param name="customerId">Customer identifier; "" to load all records</param>
    /// <param name="approved">A value indicating whether to content is approved; null to load all records</param>
    /// <param name="fromUtc">Item creation from; null to load all records</param>
    /// <param name="toUtc">Item item creation to; null to load all records</param>
    /// <param name="message">Search title or review text; null to load all records</param>
    /// <param name="storeId">Store ident</param>
    /// <param name="productId">Product ident</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Reviews</returns>
    public virtual async Task<IPagedList<ProductReview>> GetAllProductReviews(string customerId, bool? approved,
        DateTime? fromUtc = null, DateTime? toUtc = null,
        string message = null, string storeId = "", string productId = "", int pageIndex = 0,
        int pageSize = int.MaxValue)
    {
        var query = from p in _productReviewRepository.Table
            select p;

        if (approved.HasValue)
            query = query.Where(c => c.IsApproved == approved.Value);
        if (!string.IsNullOrEmpty(customerId))
            query = query.Where(c => c.CustomerId == customerId);
        if (fromUtc.HasValue)
            query = query.Where(c => fromUtc.Value <= c.CreatedOnUtc);
        if (toUtc.HasValue)
            query = query.Where(c => toUtc.Value >= c.CreatedOnUtc);
        if (!string.IsNullOrEmpty(message))
            query = query.Where(c => c.Title.Contains(message) || c.ReviewText.Contains(message));
        if (!string.IsNullOrEmpty(storeId))
            query = query.Where(c => c.StoreId == storeId || c.StoreId == "");
        if (!string.IsNullOrEmpty(productId))
            query = query.Where(c => c.ProductId == productId);

        query = query.OrderByDescending(c => c.CreatedOnUtc);

        return await PagedList<ProductReview>.Create(query, pageIndex, pageSize);
    }

    /// <summary>
    ///     Inserts a product review
    /// </summary>
    /// <param name="productReview">Product review</param>
    public virtual async Task InsertProductReview(ProductReview productReview)
    {
        ArgumentNullException.ThrowIfNull(productReview);

        await _productReviewRepository.InsertAsync(productReview);

        //event notification
        await _mediator.EntityInserted(productReview);
    }

    public virtual async Task UpdateProductReview(ProductReview productReview)
    {
        ArgumentNullException.ThrowIfNull(productReview);

        var update = UpdateBuilder<ProductReview>.Create()
            .Set(x => x.Title, productReview.Title)
            .Set(x => x.ReviewText, productReview.ReviewText)
            .Set(x => x.ReplyText, productReview.ReplyText)
            .Set(x => x.Signature, productReview.Signature)
            .Set(x => x.IsApproved, productReview.IsApproved)
            .Set(x => x.HelpfulNoTotal, productReview.HelpfulNoTotal)
            .Set(x => x.HelpfulYesTotal, productReview.HelpfulYesTotal)
            .Set(x => x.ProductReviewHelpfulnessEntries, productReview.ProductReviewHelpfulnessEntries);

        await _productReviewRepository.UpdateOneAsync(x => x.Id == productReview.Id, update);

        //event notification
        await _mediator.EntityUpdated(productReview);
    }

    /// <summary>
    ///     Deletes a product review
    /// </summary>
    /// <param name="productReview">Product review</param>
    public virtual async Task DeleteProductReview(ProductReview productReview)
    {
        ArgumentNullException.ThrowIfNull(productReview);

        await _productReviewRepository.DeleteAsync(productReview);

        //event notification
        await _mediator.EntityDeleted(productReview);
    }


    /// <summary>
    ///     Gets product review
    /// </summary>
    /// <param name="productReviewId">Product review identifier</param>
    /// <returns>Product review</returns>
    public virtual Task<ProductReview> GetProductReviewById(string productReviewId)
    {
        return _productReviewRepository.GetByIdAsync(productReviewId);
    }

    #region Fields

    private readonly IRepository<ProductReview> _productReviewRepository;
    private readonly IMediator _mediator;

    #endregion
}