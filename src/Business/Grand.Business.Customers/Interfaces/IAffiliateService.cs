using Grand.Domain;
using Grand.Domain.Affiliates;
using System;
using System.Threading.Tasks;

namespace Grand.Business.Customers.Interfaces
{
    /// <summary>
    /// Affiliate service interface
    /// </summary>
    public partial interface IAffiliateService
    {
        /// <summary>
        /// Gets an affiliate by identifier
        /// </summary>
        /// <param name="affiliateId">Affiliate identifier</param>
        /// <returns>Affiliate</returns>
        Task<Affiliate> GetAffiliateById(string affiliateId);

        /// <summary>
        /// Gets an affiliate by url name
        /// </summary>
        /// <param name="friendlyUrlName">Friendly url name</param>
        /// <returns>Affiliate</returns>
        Task<Affiliate> GetAffiliateByFriendlyUrlName(string friendlyUrlName);

        /// <summary>
        /// Gets all affiliates
        /// </summary>
        /// <returns>Affiliates</returns>
        Task<IPagedList<Affiliate>> GetAllAffiliates(string friendlyUrlName = null,
            string firstName = null, string lastName = null,
            bool loadOnlyWithOrders = false,
            DateTime? ordersCreatedFromUtc = null, DateTime? ordersCreatedToUtc = null,
            int pageIndex = 0, int pageSize = int.MaxValue,
            bool showHidden = false);

        /// <summary>
        /// Inserts an affiliate
        /// </summary>
        /// <param name="affiliate">Affiliate</param>
        Task InsertAffiliate(Affiliate affiliate);

        /// <summary>
        /// Updates the affiliate
        /// </summary>
        /// <param name="affiliate">Affiliate</param>
        Task UpdateAffiliate(Affiliate affiliate);
        /// <summary>
        /// Marks affiliate as deleted 
        /// </summary>
        /// <param name="affiliate">Affiliate</param>
        Task DeleteAffiliate(Affiliate affiliate);

    }
}