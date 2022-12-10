using Grand.Domain.Pages;

namespace Grand.Business.Core.Interfaces.Cms
{
    /// <summary>
    /// Page service interface
    /// </summary>
    public interface IPageService
    {
       
        /// <summary>
        /// Gets a page
        /// </summary>
        /// <param name="pageId">The page identifier</param>
        /// <returns>Page</returns>
        Task<Page> GetPageById(string pageId);

        /// <summary>
        /// Gets a page
        /// </summary>
        /// <param name="systemName">The page system name</param>
        /// <param name="storeId">Store identifier; pass 0 to ignore filtering by store and load the first one</param>
        /// <returns>Page</returns>
        Task<Page> GetPageBySystemName(string systemName, string storeId = "");

        /// <summary>
        /// Gets all pages
        /// </summary>
        /// <param name="storeId">Store identifier; pass "" to load all records</param>
        /// <param name="ignoreAcl"></param>
        /// <returns>Pages</returns>
        Task<IList<Page>> GetAllPages(string storeId, bool ignoreAcl = false);

        /// <summary>
        /// Inserts a page
        /// </summary>
        /// <param name="page">Page</param>
        Task InsertPage(Page page);

        /// <summary>
        /// Updates the page
        /// </summary>
        /// <param name="page">Page</param>
        Task UpdatePage(Page page);
        /// <summary>
        /// Deletes a page
        /// </summary>
        /// <param name="page">Page</param>
        Task DeletePage(Page page);

    }
}
