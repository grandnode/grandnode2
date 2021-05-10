using Grand.Domain.Pages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Cms.Interfaces
{
    /// <summary>
    /// Page layout service interface
    /// </summary>
    public partial interface IPageLayoutService
    {
        
        /// <summary>
        /// Gets all page layouts
        /// </summary>
        /// <returns>Page layouts</returns>
        Task<IList<PageLayout>> GetAllPageLayouts();

        /// <summary>
        /// Gets a page layout
        /// </summary>
        /// <param name="pageLayoutId">Page layout identifier</param>
        /// <returns>Page layout</returns>
        Task<PageLayout> GetPageLayoutById(string pageLayoutId);

        /// <summary>
        /// Inserts page layout
        /// </summary>
        /// <param name="pageLayout">Page layout</param>
        Task InsertPageLayout(PageLayout pageLayout);

        /// <summary>
        /// Updates the page layout
        /// </summary>
        /// <param name="pageLayout">Page layout</param>
        Task UpdatePageLayout(PageLayout pageLayout);
        /// <summary>
        /// Delete page layout
        /// </summary>
        /// <param name="pageLayout">Page layout</param>
        Task DeletePageLayout(PageLayout pageLayout);

    }
}
