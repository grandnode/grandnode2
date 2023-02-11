using Grand.Domain;
using Grand.Domain.News;

namespace Grand.Business.Core.Interfaces.Cms
{
    /// <summary>
    /// News service interface
    /// </summary>
    public interface INewsService
    {
        /// <summary>
        /// Gets a news
        /// </summary>
        /// <param name="newsId">The news identifier</param>
        /// <returns>News</returns>
        Task<NewsItem> GetNewsById(string newsId);

        /// <summary>
        /// Gets all news
        /// </summary>
        /// <param name="storeId">Store identifier; 0 if you want to get all records</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="ignoreAcl"></param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <param name="newsTitle">News title</param>
        /// <returns>News items</returns>
        Task<IPagedList<NewsItem>> GetAllNews(string storeId = "",
            int pageIndex = 0, int pageSize = int.MaxValue, bool ignoreAcl = false, bool showHidden = false, string newsTitle = "");

        /// <summary>
        /// Inserts a news item
        /// </summary>
        /// <param name="news">News item</param>
        Task InsertNews(NewsItem news);

        /// <summary>
        /// Updates the news item
        /// </summary>
        /// <param name="news">News item</param>
        Task UpdateNews(NewsItem news);

        /// <summary>
        /// Deletes a news
        /// </summary>
        /// <param name="newsItem">News item</param>
        Task DeleteNews(NewsItem newsItem);

        /// <summary>
        /// Gets all comments
        /// </summary>
        /// <param name="customerId">Customer identifier; "" to load all records</param>
        /// <returns>Comments</returns>
        Task<IList<NewsComment>> GetAllComments(string customerId);

        
        
    }
}
