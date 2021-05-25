using Grand.Business.Customers.Interfaces;
using Grand.Domain.Affiliates;
using Grand.Domain.Data;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Common.Filters
{
    /// <summary>
    /// Represents filter attribute that checks and updates affiliate of customer
    /// </summary>
    public class AffiliateAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Create instance of the filter attribute
        /// </summary>
        public AffiliateAttribute() : base(typeof(AffiliateFilter))
        {
        }

        #region Filter

        /// <summary>
        /// Represents a filter that checks and updates affiliate of customer
        /// </summary>
        private class AffiliateFilter : IAsyncActionFilter
        {
            #region Constants

            private const string ID_QUERY_PARAMETER_NAME = "affiliateid";
            private const string FRIENDLYURLNAME_QUERY_PARAMETER_NAME = "affiliate";

            #endregion

            #region Fields

            private readonly IAffiliateService _affiliateService;
            private readonly ICustomerService _customerService;
            private readonly IWorkContext _workContext;

            #endregion

            #region Ctor

            public AffiliateFilter(IAffiliateService affiliateService,
                ICustomerService customerService,
                IWorkContext workContext)
            {
                _affiliateService = affiliateService;
                _customerService = customerService;
                _workContext = workContext;
            }

            #endregion

            #region Utilities

            /// <summary>
            /// Set the affiliate identifier of current customer
            /// </summary>
            /// <param name="affiliate">Affiliate</param>
            protected async Task SetCustomerAffiliateId(Affiliate affiliate)
            {
                if (affiliate == null || !affiliate.Active)
                    return;

                if (affiliate.Id == _workContext.CurrentCustomer.AffiliateId)
                    return;

                //update affiliate identifier
                _workContext.CurrentCustomer.AffiliateId = affiliate.Id;
                await _customerService.UpdateCustomerField(_workContext.CurrentCustomer.Id, x => x.AffiliateId, _workContext.CurrentCustomer.AffiliateId);
            }

            #endregion

            #region Methods

            /// <summary>
            /// Called before the action executes, after model binding is complete
            /// </summary>
            /// <param name="context">A context for action filters</param>
            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                await next();

                if (context == null || context.HttpContext == null || context.HttpContext.Request == null)
                    return;

                //check request query parameters
                var request = context.HttpContext.Request;
                if (request?.Query == null || !request.Query.Any())
                    return;

                if (!DataSettingsManager.DatabaseIsInstalled())
                    return;

                //try to find by ID
                var affiliateIds = request.Query[ID_QUERY_PARAMETER_NAME];
                if (affiliateIds.Any())
                {
                    string affiliateId = affiliateIds.FirstOrDefault();
                    if (!string.IsNullOrEmpty(affiliateId))
                        await SetCustomerAffiliateId(await _affiliateService.GetAffiliateById(affiliateId));
                    return;
                }

                //try to find by friendly name
                var affiliateNames = request.Query[FRIENDLYURLNAME_QUERY_PARAMETER_NAME];
                if (affiliateNames.Any())
                {
                    var affiliateName = affiliateNames.FirstOrDefault();
                    if (!string.IsNullOrEmpty(affiliateName))
                        await SetCustomerAffiliateId(await _affiliateService.GetAffiliateByFriendlyUrlName(affiliateName));
                }
            }

            #endregion
        }

        #endregion
    }
}