using Grand.Business.Core.Interfaces.Customers;
using Grand.Data;
using Grand.Domain.Affiliates;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Grand.Web.Common.Filters;

/// <summary>
///     Represents filter attribute that checks and updates affiliate of customer
/// </summary>
public class AffiliateAttribute : TypeFilterAttribute
{
    /// <summary>
    ///     Create instance of the filter attribute
    /// </summary>
    public AffiliateAttribute() : base(typeof(AffiliateFilter))
    {
    }

    #region Filter

    /// <summary>
    ///     Represents a filter that checks and updates affiliate of customer
    /// </summary>
    private class AffiliateFilter : IAsyncActionFilter
    {
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

        #region Methods

        /// <summary>
        ///     Called before the action executes, after model binding is complete
        /// </summary>
        /// <param name="context">A context for action filters</param>
        /// <param name="next">Action execution delegate</param>
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await next();

            if (!DataSettingsManager.DatabaseIsInstalled())
                return;

            //check request query parameters
            var request = context.HttpContext.Request;
            if (!request.Query.Any())
                return;


            //try to find by ID
            var affiliateIds = request.Query[IdQueryParameterName];
            if (affiliateIds.Any())
            {
                var affiliateId = affiliateIds.FirstOrDefault();
                if (!string.IsNullOrEmpty(affiliateId))
                    await SetCustomerAffiliateId(await _affiliateService.GetAffiliateById(affiliateId));
                return;
            }

            //try to find by friendly name
            var affiliateNames = request.Query[FriendlyUrlNameQueryParameterName];
            if (affiliateNames.Any())
            {
                var affiliateName = affiliateNames.FirstOrDefault();
                if (!string.IsNullOrEmpty(affiliateName))
                    await SetCustomerAffiliateId(await _affiliateService.GetAffiliateByFriendlyUrlName(affiliateName));
            }
        }

        #endregion

        #region Utilities

        /// <summary>
        ///     Set the affiliate identifier of current customer
        /// </summary>
        /// <param name="affiliate">Affiliate</param>
        private async Task SetCustomerAffiliateId(Affiliate affiliate)
        {
            if (affiliate is not { Active: true })
                return;

            if (affiliate.Id == _workContext.CurrentCustomer.AffiliateId)
                return;

            //update affiliate identifier
            _workContext.CurrentCustomer.AffiliateId = affiliate.Id;
            await _customerService.UpdateCustomerField(_workContext.CurrentCustomer.Id, x => x.AffiliateId,
                _workContext.CurrentCustomer.AffiliateId);
        }

        #endregion

        #region Constants

        private const string IdQueryParameterName = "affiliateid";
        private const string FriendlyUrlNameQueryParameterName = "affiliate";

        #endregion

        #region Fields

        private readonly IAffiliateService _affiliateService;
        private readonly ICustomerService _customerService;
        private readonly IWorkContext _workContext;

        #endregion
    }

    #endregion
}