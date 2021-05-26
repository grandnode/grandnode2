using Grand.Business.Cms.Interfaces;
using Grand.Business.Common.Interfaces.Security;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Pages;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Cms.Services
{
    /// <summary>
    /// Page service
    /// </summary>
    public partial class PageService : IPageService
    {
        #region Fields

        private readonly IRepository<Page> _pageRepository;
        private readonly IWorkContext _workContext;
        private readonly IAclService _aclService;
        private readonly IMediator _mediator;
        private readonly ICacheBase _cacheBase;

        #endregion

        #region Ctor

        public PageService(IRepository<Page> pageRepository,
            IWorkContext workContext,
            IAclService aclService,
            IMediator mediator,
            ICacheBase cacheBase)
        {
            _pageRepository = pageRepository;
            _workContext = workContext;
            _aclService = aclService;
            _mediator = mediator;
            _cacheBase = cacheBase;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a page
        /// </summary>
        /// <param name="pageId">The page identifier</param>
        /// <returns>Page</returns>
        public virtual Task<Page> GetPageById(string pageId)
        {
            string key = string.Format(CacheKey.PAGES_BY_ID_KEY, pageId);
            return _cacheBase.GetAsync(key, () => _pageRepository.GetByIdAsync(pageId));
        }

        /// <summary>
        /// Gets a page
        /// </summary>
        /// <param name="systemName">The page system name</param>
        /// <param name="storeId">Store identifier; pass 0 to ignore filtering by store and load the first one</param>
        /// <returns>Page</returns>
        public virtual async Task<Page> GetPageBySystemName(string systemName, string storeId = "")
        {
            if (string.IsNullOrEmpty(systemName))
                return null;

            string key = string.Format(CacheKey.PAGES_BY_SYSTEMNAME, systemName, storeId);
            return await _cacheBase.GetAsync(key, async () =>
            {

                var query = from p in _pageRepository.Table
                            select p;

                query = query.Where(t => t.SystemName.ToLower() == systemName.ToLower());
                query = query.OrderBy(t => t.Id);
                var pages = await Task.FromResult(query.ToList());
                if (!String.IsNullOrEmpty(storeId))
                {
                    pages = pages.Where(x => _aclService.Authorize(x, storeId)).ToList();
                }
                return pages.FirstOrDefault();
            });
        }

        /// <summary>
        /// Gets all pages
        /// </summary>
        /// <param name="storeId">Store identifier; pass "" to load all records</param>
        /// <returns>Pages</returns>
        public virtual async Task<IList<Page>> GetAllPages(string storeId, bool ignorAcl = false)
        {
            string key = string.Format(CacheKey.PAGES_ALL_KEY, storeId, ignorAcl);
            return await _cacheBase.GetAsync(key, async () =>
            {
                var query = from p in _pageRepository.Table
                            select p;

                query = query.OrderBy(t => t.DisplayOrder).ThenBy(t => t.SystemName);

                if ((!String.IsNullOrEmpty(storeId) && !CommonHelper.IgnoreStoreLimitations) ||
                    (!ignorAcl && !CommonHelper.IgnoreAcl))
                {
                    if (!ignorAcl && !CommonHelper.IgnoreAcl)
                    {
                        var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                        query = from p in query
                                where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                                select p;
                    }
                    //Store acl
                    if (!String.IsNullOrEmpty(storeId) && !CommonHelper.IgnoreStoreLimitations)
                    {
                        query = from p in query
                                where !p.LimitedToStores || p.Stores.Contains(storeId)
                                select p;

                        query = query.OrderBy(t => t.SystemName);
                    }
                }
                return await Task.FromResult(query.ToList());
            });
        }

        /// <summary>
        /// Inserts a page
        /// </summary>
        /// <param name="page">Page</param>
        public virtual async Task InsertPage(Page page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            await _pageRepository.InsertAsync(page);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.PAGES_PATTERN_KEY);
            //event notification
            await _mediator.EntityInserted(page);
        }

        /// <summary>
        /// Updates the page
        /// </summary>
        /// <param name="page">Page</param>
        public virtual async Task UpdatePage(Page page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            await _pageRepository.UpdateAsync(page);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.PAGES_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(page);
        }
        /// <summary>
        /// Deletes a page
        /// </summary>
        /// <param name="page">Page</param>
        public virtual async Task DeletePage(Page page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            await _pageRepository.DeleteAsync(page);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.PAGES_PATTERN_KEY);
            //event notification
            await _mediator.EntityDeleted(page);
        }
        #endregion
    }
}
