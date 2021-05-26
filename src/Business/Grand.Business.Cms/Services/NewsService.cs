using Grand.Business.Cms.Interfaces;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.News;
using Grand.Infrastructure;
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
    /// News service
    /// </summary>
    public partial class NewsService : INewsService
    {
        #region Fields

        private readonly IRepository<NewsItem> _newsItemRepository;
        private readonly IMediator _mediator;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public NewsService(IRepository<NewsItem> newsItemRepository,
            IMediator mediator,
            IWorkContext workContext)
        {
            _newsItemRepository = newsItemRepository;
            _mediator = mediator;
            _workContext = workContext;
        }

        #endregion

        #region Methods


        /// <summary>
        /// Gets a news
        /// </summary>
        /// <param name="newsId">The news identifier</param>
        /// <returns>News</returns>
        public virtual Task<NewsItem> GetNewsById(string newsId)
        {
            return _newsItemRepository.GetByIdAsync(newsId);
        }

        /// <summary>
        /// Gets all news
        /// </summary>
        /// <param name="storeId">Store identifier; 0 if you want to get all records</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <param name="newsTitle">News title</param>
        /// <returns>News items</returns>
        public virtual async Task<IPagedList<NewsItem>> GetAllNews(string storeId = "",
            int pageIndex = 0, int pageSize = int.MaxValue, bool ignorAcl = false, bool showHidden = false, string newsTitle = "")
        {
            var query = from p in _newsItemRepository.Table
                        select p;

            if (!String.IsNullOrWhiteSpace(newsTitle))
                query = query.Where(n => n.Title != null && n.Title.ToLower().Contains(newsTitle.ToLower()));

            if (!showHidden)
            {
                var utcNow = DateTime.UtcNow;
                query = query.Where(n => n.Published);
                query = query.Where(n => !n.StartDateUtc.HasValue || n.StartDateUtc <= utcNow);
                query = query.Where(n => !n.EndDateUtc.HasValue || n.EndDateUtc >= utcNow);
            }

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
                }
            }
            query = query.OrderByDescending(n => n.CreatedOnUtc);
            return await PagedList<NewsItem>.Create(query, pageIndex, pageSize);
        }

        /// <summary>
        /// Inserts a news item
        /// </summary>
        /// <param name="news">News item</param>
        public virtual async Task InsertNews(NewsItem news)
        {
            if (news == null)
                throw new ArgumentNullException(nameof(news));

            await _newsItemRepository.InsertAsync(news);

            //event notification
            await _mediator.EntityInserted(news);
        }

        /// <summary>
        /// Updates the news item
        /// </summary>
        /// <param name="news">News item</param>
        public virtual async Task UpdateNews(NewsItem news)
        {
            if (news == null)
                throw new ArgumentNullException(nameof(news));

            await _newsItemRepository.UpdateAsync(news);

            //event notification
            await _mediator.EntityUpdated(news);
        }
        /// <summary>
        /// Deletes a news
        /// </summary>
        /// <param name="newsItem">News item</param>
        public virtual async Task DeleteNews(NewsItem newsItem)
        {
            if (newsItem == null)
                throw new ArgumentNullException(nameof(newsItem));

            await _newsItemRepository.DeleteAsync(newsItem);

            //event notification
            await _mediator.EntityDeleted(newsItem);
        }

        /// <summary>
        /// Gets all comments
        /// </summary>
        /// <param name="customerId">Customer identifier; "" to load all records</param>
        /// <returns>Comments</returns>
        public virtual async Task<IList<NewsComment>> GetAllComments(string customerId)
        {
            var query = from n in _newsItemRepository.Table
                        from c in n.NewsComments
                        select c;

            var query2 = from c in query
                         orderby c.CreatedOnUtc
                         where (customerId == "" || c.CustomerId == customerId)
                         select c;

            return await Task.FromResult(query2.ToList());
        }

        #endregion
    }
}
