using Grand.Domain.Pages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Cms.Interfaces
{
    /// <summary>
    /// Page service interface
    /// </summary>
    public partial interface IPageService
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
        /// <returns>Pages</returns>
        Task<IList<Page>> GetAllPages(string storeId, bool ignorAcl = false);

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
