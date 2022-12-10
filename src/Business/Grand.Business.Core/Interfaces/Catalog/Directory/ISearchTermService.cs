using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain;
using Grand.Domain.Common;

namespace Grand.Business.Core.Interfaces.Catalog.Directory
{
    /// <summary>
    /// Search term service interface
    /// </summary>
    public interface ISearchTermService
    {
        /// <summary>
        /// Deletes a search term record
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        Task DeleteSearchTerm(SearchTerm searchTerm);

        /// <summary>
        /// Gets a search term record by identifier
        /// </summary>
        /// <param name="searchTermId">Search term identifier</param>
        /// <returns>Search term</returns>
        Task<SearchTerm> GetSearchTermById(string searchTermId);

        /// <summary>
        /// Gets a search term record by keyword
        /// </summary>
        /// <param name="keyword">Search term keyword</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>Search term</returns>
        Task<SearchTerm> GetSearchTermByKeyword(string keyword, string storeId);

        /// <summary>
        /// Gets a search term statistics
        /// </summary>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>A list search term report lines</returns>
        Task<IPagedList<SearchTermReportLine>> GetStats(int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Inserts a search term record
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        Task InsertSearchTerm(SearchTerm searchTerm);

        /// <summary>
        /// Updates the search term record
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        Task UpdateSearchTerm(SearchTerm searchTerm);
    }
}