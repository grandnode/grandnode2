using Grand.Business.Cms.Interfaces;
using Grand.Domain.Data;
using Grand.Domain.Pages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Cms.Services
{
    /// <summary>
    /// Page layout service
    /// </summary>
    public partial class PageLayoutService : IPageLayoutService
    {
        #region Fields

        private readonly IRepository<PageLayout> _pageLayoutRepository;
        private readonly ICacheBase _cacheBase;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="pageLayoutRepository">Page layout repository</param>
        /// <param name="cacheBase">cache base</param>
        /// <param name="mediator">Mediator</param>
        public PageLayoutService(IRepository<PageLayout> pageLayoutRepository,
            ICacheBase cacheBase,
            IMediator mediator)
        {
            _pageLayoutRepository = pageLayoutRepository;
            _cacheBase = cacheBase;
            _mediator = mediator;
        }

        #endregion

        #region Methods

       
        /// <summary>
        /// Gets all page layouts
        /// </summary>
        /// <returns>Page layouts</returns>
        public virtual async Task<IList<PageLayout>> GetAllPageLayouts()
        {
            return await _cacheBase.GetAsync(CacheKey.PAGE_LAYOUT_ALL, async () =>
            {
                var query = from pt in _pageLayoutRepository.Table
                            orderby pt.DisplayOrder
                            select pt;

                return await Task.FromResult(query.ToList());
            });
        }

        /// <summary>
        /// Gets a page layout
        /// </summary>
        /// <param name="pageLayoutId">Page layout identifier</param>
        /// <returns>Page layout</returns>
        public virtual Task<PageLayout> GetPageLayoutById(string pageLayoutId)
        {
            string key = string.Format(CacheKey.PAGE_LAYOUT_BY_ID_KEY, pageLayoutId);
            return _cacheBase.GetAsync(key, () => _pageLayoutRepository.GetByIdAsync(pageLayoutId));
        }

        /// <summary>
        /// Inserts page layout
        /// </summary>
        /// <param name="pageLayout">Page layout</param>
        public virtual async Task InsertPageLayout(PageLayout pageLayout)
        {
            if (pageLayout == null)
                throw new ArgumentNullException(nameof(pageLayout));

            await _pageLayoutRepository.InsertAsync(pageLayout);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.PAGE_LAYOUT_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(pageLayout);
        }

        /// <summary>
        /// Updates the page layout
        /// </summary>
        /// <param name="pageLayout">Page layout</param>
        public virtual async Task UpdatePageLayout(PageLayout pageLayout)
        {
            if (pageLayout == null)
                throw new ArgumentNullException(nameof(pageLayout));

            await _pageLayoutRepository.UpdateAsync(pageLayout);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.PAGE_LAYOUT_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(pageLayout);
        }
        /// <summary>
        /// Delete page layout
        /// </summary>
        /// <param name="pageLayout">Page layout</param>
        public virtual async Task DeletePageLayout(PageLayout pageLayout)
        {
            if (pageLayout == null)
                throw new ArgumentNullException(nameof(pageLayout));

            await _pageLayoutRepository.DeleteAsync(pageLayout);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.PAGE_LAYOUT_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(pageLayout);
        }

        #endregion
    }
}
