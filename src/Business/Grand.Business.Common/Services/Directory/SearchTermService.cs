using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Utilities;
using Grand.Domain;
using Grand.Domain.Common;
using Grand.Domain.Data;
using Grand.Infrastructure.Extensions;
using MediatR;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Common.Services.Directory
{
    /// <summary>
    /// Search term service
    /// </summary>
    public partial class SearchTermService : ISearchTermService
    {
        #region Fields

        private readonly IRepository<SearchTerm> _searchTermRepository;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        public SearchTermService(IRepository<SearchTerm> searchTermRepository,
            IMediator mediator)
        {
            _searchTermRepository = searchTermRepository;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Deletes a search term record
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        public virtual async Task DeleteSearchTerm(SearchTerm searchTerm)
        {
            if (searchTerm == null)
                throw new ArgumentNullException(nameof(searchTerm));

            await _searchTermRepository.DeleteAsync(searchTerm);

            //event notification
            await _mediator.EntityDeleted(searchTerm);
        }

        /// <summary>
        /// Gets a search term record by identifier
        /// </summary>
        /// <param name="searchTermId">Search term identifier</param>
        /// <returns>Search term</returns>
        public virtual Task<SearchTerm> GetSearchTermById(string searchTermId)
        {
            return _searchTermRepository.GetByIdAsync(searchTermId);
        }

        /// <summary>
        /// Gets a search term record by keyword
        /// </summary>
        /// <param name="keyword">Search term keyword</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>Search term</returns>
        public virtual async Task<SearchTerm> GetSearchTermByKeyword(string keyword, string storeId)
        {
            if (String.IsNullOrEmpty(keyword))
                return null;

            var query = from st in _searchTermRepository.Table
                        where st.Keyword == keyword && st.StoreId == storeId
                        select st;
            return await Task.FromResult(query.FirstOrDefault());
        }

        /// <summary>
        /// Gets a search term statistics
        /// </summary>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>A list search term report lines</returns>
        public virtual async Task<IPagedList<SearchTermReportLine>> GetStats(int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = (from st in _searchTermRepository.Table
                         group st by st.Keyword into groupedResult
                         select new
                         {
                             Keyword = groupedResult.Key,
                             Count = groupedResult.Sum(o => o.Count)
                         })
                        .OrderByDescending(m => m.Count)
                        .Select(r => new SearchTermReportLine
                        {
                            Keyword = r.Keyword,
                            Count = r.Count
                        });
            return await PagedList<SearchTermReportLine>.Create(query, pageIndex, pageSize);
        }

        /// <summary>
        /// Inserts a search term record
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        public virtual async Task InsertSearchTerm(SearchTerm searchTerm)
        {
            if (searchTerm == null)
                throw new ArgumentNullException(nameof(searchTerm));

            await _searchTermRepository.InsertAsync(searchTerm);

            //event notification
            await _mediator.EntityInserted(searchTerm);
        }

        /// <summary>
        /// Updates the search term record
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        public virtual async Task UpdateSearchTerm(SearchTerm searchTerm)
        {
            if (searchTerm == null)
                throw new ArgumentNullException(nameof(searchTerm));

            await _searchTermRepository.UpdateAsync(searchTerm);

            //event notification
            await _mediator.EntityUpdated(searchTerm);
        }

        #endregion
    }
}