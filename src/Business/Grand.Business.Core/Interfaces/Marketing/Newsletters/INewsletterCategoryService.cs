using Grand.Domain.Messages;

namespace Grand.Business.Core.Interfaces.Marketing.Newsletters;

public interface INewsletterCategoryService
{
    /// <summary>
    ///     Inserts a newsletter category
    /// </summary>
    /// <param name="newsletterCategory">NewsletterCategory</param>
    Task InsertNewsletterCategory(NewsletterCategory newsletterCategory);

    /// <summary>
    ///     Updates a newsletter category
    /// </summary>
    /// <param name="newsletterCategory">NewsletterCategory</param>
    Task UpdateNewsletterCategory(NewsletterCategory newsletterCategory);

    /// <summary>
    ///     Deleted a newsletter category
    /// </summary>
    /// <param name="newsletterCategory">NewsletterCategory</param>
    Task DeleteNewsletterCategory(NewsletterCategory newsletterCategory);

    /// <summary>
    ///     Gets a newsletter by category
    /// </summary>
    /// <param name="id">newsletter category</param>
    /// <returns>NewsletterCategory</returns>
    Task<NewsletterCategory> GetNewsletterCategoryById(string id);

    /// <summary>
    ///     Gets all newsletter categories
    /// </summary>
    /// <returns>NewsletterCategories</returns>
    Task<IList<NewsletterCategory>> GetAllNewsletterCategory();

    /// <summary>
    ///     Gets all newsletter categories by store
    /// </summary>
    /// <returns>NewsletterCategories</returns>
    Task<IList<NewsletterCategory>> GetNewsletterCategoriesByStore(string storeId);
}