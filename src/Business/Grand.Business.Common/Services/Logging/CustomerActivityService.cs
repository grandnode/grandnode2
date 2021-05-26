using Grand.Business.Common.Interfaces.Logging;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Logging;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.SharedKernel.Extensions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Common.Services.Logging
{
    /// <summary>
    /// Customer activity service
    /// </summary>
    public class CustomerActivityService : ICustomerActivityService
    {
        #region Fields

        /// <summary>
        /// Cache manager
        /// </summary>
        private readonly ICacheBase _cacheBase;
        private readonly IRepository<ActivityLog> _activityLogRepository;
        private readonly IRepository<ActivityLogType> _activityLogTypeRepository;
        private readonly IWorkContext _workContext;
        private readonly IActivityKeywordsProvider _activityKeywordsProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;

        #endregion

        #region Ctor
        /// <summary>
        /// Ctor
        /// </summary>

        public CustomerActivityService(
            ICacheBase cacheBase,
            IRepository<ActivityLog> activityLogRepository,
            IRepository<ActivityLogType> activityLogTypeRepository,
            IWorkContext workContext,
            IActivityKeywordsProvider activityKeywordsProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            _cacheBase = cacheBase;
            _activityLogRepository = activityLogRepository;
            _activityLogTypeRepository = activityLogTypeRepository;
            _workContext = workContext;
            _activityKeywordsProvider = activityKeywordsProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        #endregion

        /// <summary>
        /// Gets all activity log types (to caching)
        /// </summary>
        /// <returns>Activity log types</returns>
        protected virtual async Task<IList<ActivityLogType>> GetAllActivityTypesCached()
        {
            //cache
            string key = string.Format(CacheKey.ACTIVITYTYPE_ALL_KEY);
            return await _cacheBase.GetAsync(key, async () =>
            {
                return await GetAllActivityTypes();
            });
        }


        #region Methods

        /// <summary>
        /// Inserts an activity log type item
        /// </summary>
        /// <param name="activityLogType">Activity log type item</param>
        public virtual async Task InsertActivityType(ActivityLogType activityLogType)
        {
            if (activityLogType == null)
                throw new ArgumentNullException(nameof(activityLogType));

            await _activityLogTypeRepository.InsertAsync(activityLogType);
            await _cacheBase.RemoveByPrefix(CacheKey.ACTIVITYTYPE_PATTERN_KEY);
        }

        /// <summary>
        /// Updates an activity log type item
        /// </summary>
        /// <param name="activityLogType">Activity log type item</param>
        public virtual async Task UpdateActivityType(ActivityLogType activityLogType)
        {
            if (activityLogType == null)
                throw new ArgumentNullException(nameof(activityLogType));

            await _activityLogTypeRepository.UpdateAsync(activityLogType);
            await _cacheBase.RemoveByPrefix(CacheKey.ACTIVITYTYPE_PATTERN_KEY);
        }

        /// <summary>
        /// Deletes an activity log type item
        /// </summary>
        /// <param name="activityLogType">Activity log type</param>
        public virtual async Task DeleteActivityType(ActivityLogType activityLogType)
        {
            if (activityLogType == null)
                throw new ArgumentNullException(nameof(activityLogType));

            await _activityLogTypeRepository.DeleteAsync(activityLogType);
            await _cacheBase.RemoveByPrefix(CacheKey.ACTIVITYTYPE_PATTERN_KEY);
        }

        /// <summary>
        /// Gets all activity log type items
        /// </summary>
        /// <returns>Activity log type items</returns>
        public virtual async Task<IList<ActivityLogType>> GetAllActivityTypes()
        {
            var query = from alt in _activityLogTypeRepository.Table
                        orderby alt.Name
                        select alt;
            return await Task.FromResult(query.ToList());
        }

        /// <summary>
        /// Gets an activity log type item
        /// </summary>
        /// <param name="activityLogTypeId">Activity log type identifier</param>
        /// <returns>Activity log type item</returns>
        public virtual async Task<ActivityLogType> GetActivityTypeById(string activityLogTypeId)
        {
            if (string.IsNullOrEmpty(activityLogTypeId))
                return null;

            var key = string.Format(CacheKey.ACTIVITYTYPE_BY_KEY, activityLogTypeId);
            return await _cacheBase.GetAsync(key, () => _activityLogTypeRepository.GetByIdAsync(activityLogTypeId));
        }

        /// <summary>
        /// Inserts an activity log item
        /// </summary>
        /// <param name="systemKeyword">The system keyword</param>
        /// <param name="comment">The activity comment</param>
        /// <param name="commentParams">The activity comment parameters for string.Format() function.</param>
        /// <returns>Activity log item</returns>
        public virtual async Task InsertActivity(string systemKeyword, string entityKeyId,
            string comment, params object[] commentParams)
        {
            await InsertActivity(systemKeyword, entityKeyId, comment, _workContext.CurrentCustomer, commentParams);
        }

        /// <summary>
        /// Inserts an activity log item
        /// </summary>
        /// <param name="systemKeyword">The system keyword</param>
        /// <param name="comment">The activity comment</param>
        /// <param name="customer">The customer</param>
        /// <param name="commentParams">The activity comment parameters for string.Format() function.</param>
        /// <returns>Activity log item</returns>
        public virtual async Task<ActivityLog> InsertActivity(string systemKeyword, string entityKeyId,
            string comment, Customer customer, params object[] commentParams)
        {
            if (customer == null)
                return null;

            var activityTypes = await GetAllActivityTypesCached();
            var activityType = activityTypes.ToList().Find(at => at.SystemKeyword == systemKeyword);
            if (activityType == null || !activityType.Enabled)
                return null;

            comment = CommonHelper.EnsureNotNull(comment);
            comment = string.Format(comment, commentParams);
            comment = CommonHelper.EnsureMaximumLength(comment, 4000);

            var activity = new ActivityLog();
            activity.ActivityLogTypeId = activityType.Id;
            activity.CustomerId = customer.Id;
            activity.EntityKeyId = entityKeyId;
            activity.Comment = comment;
            activity.CreatedOnUtc = DateTime.UtcNow;
            activity.IpAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
            await _activityLogRepository.InsertAsync(activity);

            return activity;
        }

        /// <summary>
        /// Deletes an activity log item
        /// </summary>
        /// <param name="activityLog">Activity log type</param>
        public virtual async Task DeleteActivity(ActivityLog activityLog)
        {
            if (activityLog == null)
                throw new ArgumentNullException(nameof(activityLog));

            await _activityLogRepository.DeleteAsync(activityLog);
        }

        /// <summary>
        /// Gets all activity log items
        /// </summary>
        /// <param name="comment">Log item message text or text part; null or empty string to load all</param>
        /// <param name="createdOnFrom">Log item creation from; null to load all customers</param>
        /// <param name="createdOnTo">Log item creation to; null to load all customers</param>
        /// <param name="customerId">Customer identifier; null to load all customers</param>
        /// <param name="activityLogTypeId">Activity log type identifier</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Activity log items</returns>
        public virtual async Task<IPagedList<ActivityLog>> GetAllActivities(string comment = "",
            DateTime? createdOnFrom = null,
            DateTime? createdOnTo = null, string customerId = "", string activityLogTypeId = "",
            string ipAddress = null,
            int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = from p in _activityLogRepository.Table
                        select p;

            if (!String.IsNullOrEmpty(comment))
                query = query.Where(al => al.Comment != null && al.Comment.ToLower().Contains(comment.ToLower()));
            if (createdOnFrom.HasValue)
                query = query.Where(al => createdOnFrom.Value <= al.CreatedOnUtc);
            if (createdOnTo.HasValue)
                query = query.Where(al => createdOnTo.Value >= al.CreatedOnUtc);
            if (!String.IsNullOrEmpty(activityLogTypeId))
                query = query.Where(al => activityLogTypeId == al.ActivityLogTypeId);
            if (!String.IsNullOrEmpty(customerId))
                query = query.Where(al => customerId == al.CustomerId);
            if (!String.IsNullOrEmpty(ipAddress))
                query = query.Where(al => ipAddress == al.IpAddress);

            query = query.OrderByDescending(al => al.CreatedOnUtc);
            return await PagedList<ActivityLog>.Create(query, pageIndex, pageSize);
        }

        /// <summary>
        /// Gets stats activity log items
        /// </summary>
        /// <param name="createdOnFrom">Log item creation from; null to load all records</param>
        /// <param name="createdOnTo">Log item creation to; null to load all records</param>
        /// <param name="activityLogTypeId">Activity log type identifier</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Activity log items</returns>
        public virtual async Task<IPagedList<ActivityStats>> GetStatsActivities(DateTime? createdOnFrom = null,
            DateTime? createdOnTo = null, string activityLogTypeId = "",
            int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = from p in _activityLogRepository.Table
                        select p;

            if (createdOnFrom.HasValue)
                query = query.Where(al => createdOnFrom.Value <= al.CreatedOnUtc);
            if (createdOnTo.HasValue)
                query = query.Where(al => createdOnTo.Value >= al.CreatedOnUtc);
            if (!String.IsNullOrEmpty(activityLogTypeId))
                query = query.Where(al => activityLogTypeId == al.ActivityLogTypeId);

            var gquery = query.GroupBy(key=> new { key.ActivityLogTypeId, key.EntityKeyId })
                .Select(g => new ActivityStats {
                    ActivityLogTypeId = g.Key.ActivityLogTypeId,
                    EntityKeyId = g.Key.EntityKeyId,
                    Count = g.Count(),
                });

            gquery = gquery.OrderByDescending(x => x.Count);

            return await PagedList<ActivityStats>.Create(gquery, pageIndex, pageSize);
        }
        /// <summary>
        /// Gets category activity log items
        /// </summary>
        /// <param name="createdOnFrom">Log item creation from; null to load all records</param>
        /// <param name="createdOnTo">Log item creation to; null to load all records</param>
        /// <param name="categoryId">Category identifier</param>        
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Activity log items</returns>
        public virtual async Task<IPagedList<ActivityLog>> GetCategoryActivities(DateTime? createdOnFrom = null,
            DateTime? createdOnTo = null, string categoryId = "", int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = from p in _activityLogRepository.Table
                        select p;

            if (createdOnFrom.HasValue)
                query = query.Where(al => createdOnFrom.Value <= al.CreatedOnUtc);
            if (createdOnTo.HasValue)
                query = query.Where(al => createdOnTo.Value >= al.CreatedOnUtc);

            var activityTypes = await GetAllActivityTypesCached();
            var activityTypeIds = activityTypes.ToList().Where(at => _activityKeywordsProvider.GetCategorySystemKeywords().Contains(at.SystemKeyword)).Select(x => x.Id);

            query = query.Where(al => activityTypeIds.Contains(al.ActivityLogTypeId));

            query = query.Where(al => al.EntityKeyId == categoryId);
            query = query.OrderByDescending(al => al.CreatedOnUtc);
            return await PagedList<ActivityLog>.Create(query, pageIndex, pageSize);
        }

        /// <summary>
        /// Gets knowledgebase category activity log items
        /// </summary>
        /// <param name="createdOnFrom">Log item creation from; null to load all records</param>
        /// <param name="createdOnTo">Log item creation to; null to load all records</param>
        /// <param name="categoryId">Category identifier</param>        
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Activity log items</returns>
        public virtual async Task<IPagedList<ActivityLog>> GetKnowledgebaseCategoryActivities(DateTime? createdOnFrom = null,
            DateTime? createdOnTo = null, string categoryId = "", int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = from p in _activityLogRepository.Table
                        select p;

            if (createdOnFrom.HasValue)
                query = query.Where(al => createdOnFrom.Value <= al.CreatedOnUtc);
            if (createdOnTo.HasValue)
                query = query.Where(al => createdOnTo.Value >= al.CreatedOnUtc);

            var activityTypes = await GetAllActivityTypesCached();
            var activityTypeIds = activityTypes.ToList().Where(at => _activityKeywordsProvider.GetKnowledgebaseCategorySystemKeywords().Contains(at.SystemKeyword)).Select(x => x.Id);

            query = query.Where(al => activityTypeIds.Contains(al.ActivityLogTypeId));

            query = query.Where(al => al.EntityKeyId == categoryId);
            query = query.OrderByDescending(al => al.CreatedOnUtc);
            return await PagedList<ActivityLog>.Create(query, pageIndex, pageSize);
        }

        /// <summary>
        /// Gets knowledgebase article activity log items
        /// </summary>
        /// <param name="createdOnFrom">Log item creation from; null to load all records</param>
        /// <param name="createdOnTo">Log item creation to; null to load all records</param>
        /// <param name="categoryId">Category identifier</param>        
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Activity log items</returns>
        public virtual async Task<IPagedList<ActivityLog>> GetKnowledgebaseArticleActivities(DateTime? createdOnFrom = null,
            DateTime? createdOnTo = null, string categoryId = "", int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = from p in _activityLogRepository.Table
                        select p;

            if (createdOnFrom.HasValue)
                query = query.Where(al => createdOnFrom.Value <= al.CreatedOnUtc);
            if (createdOnTo.HasValue)
                query = query.Where(al => createdOnTo.Value >= al.CreatedOnUtc);

            var activityTypes = await GetAllActivityTypesCached();
            var activityTypeIds = activityTypes.ToList().Where(at => _activityKeywordsProvider.GetKnowledgebaseArticleSystemKeywords().Contains(at.SystemKeyword)).Select(x => x.Id);

            query = query.Where(al => activityTypeIds.Contains(al.ActivityLogTypeId));

            query = query.Where(al => al.EntityKeyId == categoryId);
            query = query.OrderByDescending(al => al.CreatedOnUtc);
            return await PagedList<ActivityLog>.Create(query, pageIndex, pageSize);
        }
        /// <summary>
        /// Gets collection activity log items
        /// </summary>
        /// <param name="createdOnFrom">Log item creation from; null to load all records</param>
        /// <param name="createdOnTo">Log item creation to; null to load all records</param>
        /// <param name="brandId">Brand identifier</param>        
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Activity log items</returns>
        public virtual async Task<IPagedList<ActivityLog>> GetBrandActivities(DateTime? createdOnFrom = null,
            DateTime? createdOnTo = null, string brandId = "", int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = from p in _activityLogRepository.Table
                        select p;

            if (createdOnFrom.HasValue)
                query = query.Where(al => createdOnFrom.Value <= al.CreatedOnUtc);
            if (createdOnTo.HasValue)
                query = query.Where(al => createdOnTo.Value >= al.CreatedOnUtc);

            var activityTypes = await GetAllActivityTypesCached();
            var activityTypeIds = activityTypes.ToList().Where(at => _activityKeywordsProvider.GetBrandSystemKeywords().Contains(at.SystemKeyword)).Select(x => x.Id);

            query = query.Where(al => activityTypeIds.Contains(al.ActivityLogTypeId));

            query = query.Where(al => al.EntityKeyId == brandId);
            query = query.OrderByDescending(al => al.CreatedOnUtc);
            return await PagedList<ActivityLog>.Create(query, pageIndex, pageSize);
        }

        /// <summary>
        /// Gets collection activity log items
        /// </summary>
        /// <param name="createdOnFrom">Log item creation from; null to load all records</param>
        /// <param name="createdOnTo">Log item creation to; null to load all records</param>
        /// <param name="categoryId">Collection identifier</param>        
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Activity log items</returns>
        public virtual async Task<IPagedList<ActivityLog>> GetCollectionActivities(DateTime? createdOnFrom = null,
            DateTime? createdOnTo = null, string collectionId = "", int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = from p in _activityLogRepository.Table
                        select p;

            if (createdOnFrom.HasValue)
                query = query.Where(al => createdOnFrom.Value <= al.CreatedOnUtc);
            if (createdOnTo.HasValue)
                query = query.Where(al => createdOnTo.Value >= al.CreatedOnUtc);

            var activityTypes = await GetAllActivityTypesCached();
            var activityTypeIds = activityTypes.ToList().Where(at => _activityKeywordsProvider.GetCollectionSystemKeywords().Contains(at.SystemKeyword)).Select(x => x.Id);

            query = query.Where(al => activityTypeIds.Contains(al.ActivityLogTypeId));

            query = query.Where(al => al.EntityKeyId == collectionId);
            query = query.OrderByDescending(al => al.CreatedOnUtc);
            return await PagedList<ActivityLog>.Create(query, pageIndex, pageSize);
        }

        /// <summary>
        /// Gets product activity log items
        /// </summary>
        /// <param name="createdOnFrom">Log item creation from; null to load all records</param>
        /// <param name="createdOnTo">Log item creation to; null to load all records</param>
        /// <param name="productId">Product identifier</param>        
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Activity log items</returns>
        public virtual async Task<IPagedList<ActivityLog>> GetProductActivities(DateTime? createdOnFrom = null,
            DateTime? createdOnTo = null, string productId = "", int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = from p in _activityLogRepository.Table
                        select p;

            if (createdOnFrom.HasValue)
                query = query.Where(al => createdOnFrom.Value <= al.CreatedOnUtc);
            if (createdOnTo.HasValue)
                query = query.Where(al => createdOnTo.Value >= al.CreatedOnUtc);

            var activityTypes = await GetAllActivityTypesCached();
            var activityTypeIds = activityTypes.ToList().Where(at => _activityKeywordsProvider.GetProductSystemKeywords().Contains(at.SystemKeyword)).Select(x => x.Id);

            query = query.Where(al => activityTypeIds.Contains(al.ActivityLogTypeId));

            query = query.Where(al => al.EntityKeyId == productId);
            query = query.OrderByDescending(al => al.CreatedOnUtc);
            return await PagedList<ActivityLog>.Create(query, pageIndex, pageSize);
        }

        /// <summary>
        /// Gets an activity log item
        /// </summary>
        /// <param name="activityLogId">Activity log identifier</param>
        /// <returns>Activity log item</returns>
        public virtual Task<ActivityLog> GetActivityById(string activityLogId)
        {
            return _activityLogRepository.GetByIdAsync(activityLogId);
        }

        /// <summary>
        /// Clears activity log
        /// </summary>
        public virtual async Task ClearAllActivities()
        {
            await _activityLogRepository.ClearAsync();
        }
        #endregion

    }
}
