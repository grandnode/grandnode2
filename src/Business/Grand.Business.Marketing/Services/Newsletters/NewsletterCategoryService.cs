using Grand.Business.Core.Interfaces.Marketing.Newsletters;
using Grand.Data;
using Grand.Domain.Messages;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Marketing.Services.Newsletters;

public class NewsletterCategoryService : INewsletterCategoryService
{
    #region Ctor

    public NewsletterCategoryService(IRepository<NewsletterCategory> newsletterCategoryRepository, IMediator mediator)
    {
        _newsletterCategoryRepository = newsletterCategoryRepository;
        _mediator = mediator;
    }

    #endregion

    #region Fields

    private readonly IRepository<NewsletterCategory> _newsletterCategoryRepository;
    private readonly IMediator _mediator;

    #endregion

    #region Methods

    /// <summary>
    ///     Inserts a newsletter category
    /// </summary>
    /// <param name="newsletterCategory">NewsletterCategory</param>
    public virtual async Task InsertNewsletterCategory(NewsletterCategory newsletterCategory)
    {
        ArgumentNullException.ThrowIfNull(newsletterCategory);

        await _newsletterCategoryRepository.InsertAsync(newsletterCategory);

        //event notification
        await _mediator.EntityInserted(newsletterCategory);
    }

    /// <summary>
    ///     Updates a newsletter category
    /// </summary>
    /// <param name="newsletterCategory">NewsletterCategory</param>
    public virtual async Task UpdateNewsletterCategory(NewsletterCategory newsletterCategory)
    {
        ArgumentNullException.ThrowIfNull(newsletterCategory);

        await _newsletterCategoryRepository.UpdateAsync(newsletterCategory);

        //event notification
        await _mediator.EntityUpdated(newsletterCategory);
    }

    /// <summary>
    ///     Deleted a newsletter category
    /// </summary>
    /// <param name="newsletterCategory">NewsletterCategory</param>
    public virtual async Task DeleteNewsletterCategory(NewsletterCategory newsletterCategory)
    {
        ArgumentNullException.ThrowIfNull(newsletterCategory);

        await _newsletterCategoryRepository.DeleteAsync(newsletterCategory);

        //event notification
        await _mediator.EntityDeleted(newsletterCategory);
    }

    /// <summary>
    ///     Gets a newsletter by category
    /// </summary>
    /// <param name="id">newsletter category</param>
    /// <returns>NewsletterCategory</returns>
    public virtual Task<NewsletterCategory> GetNewsletterCategoryById(string id)
    {
        return _newsletterCategoryRepository.GetByIdAsync(id);
    }

    /// <summary>
    ///     Gets all newsletter categories
    /// </summary>
    /// <returns>NewsletterCategories</returns>
    public virtual async Task<IList<NewsletterCategory>> GetAllNewsletterCategory()
    {
        return await Task.FromResult(_newsletterCategoryRepository.Table.ToList());
    }

    /// <summary>
    ///     Gets all newsletter categories by store
    /// </summary>
    /// <returns>NewsletterCategories</returns>
    public virtual async Task<IList<NewsletterCategory>> GetNewsletterCategoriesByStore(string storeId)
    {
        var query = from p in _newsletterCategoryRepository.Table
            where !p.LimitedToStores || p.Stores.Contains(storeId)
            orderby p.DisplayOrder
            select p;
        return await Task.FromResult(query.ToList());
    }

    #endregion
}