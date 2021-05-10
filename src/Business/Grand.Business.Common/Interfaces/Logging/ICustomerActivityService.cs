using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Common.Interfaces.Logging
{
    /// <summary>
    /// Customer activity service interface
    /// </summary>
    public partial interface ICustomerActivityService
    {
        /// <summary>
        /// Inserts an activity log type item
        /// </summary>
        /// <param name="activityLogType">Activity log type item</param>
        Task InsertActivityType(ActivityLogType activityLogType);

        /// <summary>
        /// Updates an activity log type item
        /// </summary>
        /// <param name="activityLogType">Activity log type item</param>
        Task UpdateActivityType(ActivityLogType activityLogType);

        /// <summary>
        /// Deletes an activity log type item
        /// </summary>
        /// <param name="activityLogType">Activity log type</param>
        Task DeleteActivityType(ActivityLogType activityLogType);

        /// <summary>
        /// Gets all activity log type items
        /// </summary>
        /// <returns>Activity log type items</returns>
        Task<IList<ActivityLogType>> GetAllActivityTypes();

        /// <summary>
        /// Gets an activity log type item
        /// </summary>
        /// <param name="activityLogTypeId">Activity log type identifier</param>
        /// <returns>Activity log type item</returns>
        Task<ActivityLogType> GetActivityTypeById(string activityLogTypeId);

        /// <summary>
        /// Inserts an activity log item
        /// </summary>
        /// <param name="systemKeyword">The system keyword</param>
        /// <param name="comment">The activity comment</param>
        /// <param name="commentParams">The activity comment parameters for string.Format() function.</param>
        /// <returns>Activity log item</returns>
        Task InsertActivity(string systemKeyword, string entityKeyId, string comment, params object[] commentParams);

        /// <summary>
        /// Inserts an activity log item
        /// </summary>
        /// <param name="systemKeyword">The system keyword</param>
        /// <param name="comment">The activity comment</param>
        /// <param name="customer">The customer</param>
        /// <param name="commentParams">The activity comment parameters for string.Format() function.</param>
        /// <returns>Activity log item</returns>
        Task<ActivityLog> InsertActivity(string systemKeyword, string entityKeyId,
            string comment, Customer customer, params object[] commentParams);


        /// <summary>
        /// Deletes an activity log item
        /// </summary>
        /// <param name="activityLog">Activity log</param>
        Task DeleteActivity(ActivityLog activityLog);

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
        Task<IPagedList<ActivityLog>> GetAllActivities(string comment = "",
            DateTime? createdOnFrom = null,
            DateTime? createdOnTo = null, string customerId = "", string activityLogTypeId = "",
            string ipAddress = null, int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Gets stats activity log items
        /// </summary>
        /// <param name="createdOnFrom">Log item creation from; null to load all records</param>
        /// <param name="createdOnTo">Log item creation to; null to load all records</param>
        /// <param name="activityLogTypeId">Activity log type identifier</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Stats Activity log items</returns>
        Task<IPagedList<ActivityStats>> GetStatsActivities(DateTime? createdOnFrom = null,
            DateTime? createdOnTo = null, string activityLogTypeId = "",
            int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Gets category activity log items
        /// </summary>
        /// <param name="createdOnFrom">Log item creation from; null to load all records</param>
        /// <param name="createdOnTo">Log item creation to; null to load all records</param>
        /// <param name="categoryId">Category identifier</param>        
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Activity log items</returns>
        Task<IPagedList<ActivityLog>> GetCategoryActivities(DateTime? createdOnFrom = null,
            DateTime? createdOnTo = null, string categoryId = "", int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Gets knowledgebase category activity log items
        /// </summary>
        /// <param name="createdOnFrom">Log item creation from; null to load all records</param>
        /// <param name="createdOnTo">Log item creation to; null to load all records</param>
        /// <param name="categoryId">Category identifier</param>        
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Activity log items</returns>
        Task<IPagedList<ActivityLog>> GetKnowledgebaseCategoryActivities(DateTime? createdOnFrom = null,
            DateTime? createdOnTo = null, string categoryId = "", int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Gets knowledgebase article activity log items
        /// </summary>
        /// <param name="createdOnFrom">Log item creation from; null to load all records</param>
        /// <param name="createdOnTo">Log item creation to; null to load all records</param>
        /// <param name="categoryId">Category identifier</param>        
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Activity log items</returns>
        Task<IPagedList<ActivityLog>> GetKnowledgebaseArticleActivities(DateTime? createdOnFrom = null,
            DateTime? createdOnTo = null, string categoryId = "", int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Gets collection activity log items
        /// </summary>
        /// <param name="createdOnFrom">Log item creation from; null to load all records</param>
        /// <param name="createdOnTo">Log item creation to; null to load all records</param>
        /// <param name="brandId">Brand identifier</param>        
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Activity log items</returns>
        Task<IPagedList<ActivityLog>> GetBrandActivities(DateTime? createdOnFrom = null,
            DateTime? createdOnTo = null, string brandId = "", int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Gets collection activity log items
        /// </summary>
        /// <param name="createdOnFrom">Log item creation from; null to load all records</param>
        /// <param name="createdOnTo">Log item creation to; null to load all records</param>
        /// <param name="collectionId">Collection identifier</param>        
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Activity log items</returns>
        Task<IPagedList<ActivityLog>> GetCollectionActivities(DateTime? createdOnFrom = null,
            DateTime? createdOnTo = null, string collectionId = "", int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Gets product activity log items
        /// </summary>
        /// <param name="createdOnFrom">Log item creation from; null to load all records</param>
        /// <param name="createdOnTo">Log item creation to; null to load all records</param>
        /// <param name="productId">Collection identifier</param>        
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Activity log items</returns>
        Task<IPagedList<ActivityLog>> GetProductActivities(DateTime? createdOnFrom = null,
            DateTime? createdOnTo = null, string productId = "", int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Gets an activity log item
        /// </summary>
        /// <param name="activityLogId">Activity log identifier</param>
        /// <returns>Activity log item</returns>
        Task<ActivityLog> GetActivityById(string activityLogId);

        /// <summary>
        /// Clears activity log
        /// </summary>
        Task ClearAllActivities();
    }
}
