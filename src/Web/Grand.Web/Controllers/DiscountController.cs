using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.SharedKernel.Attributes;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Models.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Controllers;

[DenySystemAccount]
[ApiGroup(SharedKernel.Extensions.ApiConstants.ApiGroupNameV2)]
public class DiscountController : BasePublicController
{
    private readonly IDiscountService _discountService;
    private readonly IContextAccessor _contextAccessor;

    public DiscountController(IDiscountService discountService, IContextAccessor contextAccessor)
    {
        _discountService = discountService;
        _contextAccessor = contextAccessor;
    }

    [HttpGet]
    [CustomerGroupAuthorize(SystemCustomerGroupNames.Registered)]
    public virtual async Task<IActionResult> CustomerDiscounts()
    {
        var storeId = _contextAccessor.StoreContext.CurrentStore.Id;
        // GetActiveDiscountsByContext requires a non-null DiscountType; use GetDiscountsQuery to get all types
        var allDiscounts = await _discountService.GetDiscountsQuery(discountType: null, storeId: storeId);

        var now = DateTime.UtcNow;
        var model = allDiscounts
            .Where(d => d.IsEnabled
                && (d.StartDateUtc == null || d.StartDateUtc <= now)
                && (d.EndDateUtc == null || d.EndDateUtc >= now))
            .Take(200)
            .Select(d => new CustomerDiscountModel {
                Name = d.Name,
                UsePercentage = d.UsePercentage,
                DiscountPercentage = d.DiscountPercentage,
                DiscountAmount = d.DiscountAmount,
                CurrencyCode = d.CurrencyCode,
                EndDateUtc = d.EndDateUtc,
                RequiresCouponCode = d.RequiresCouponCode
            })
            .ToList();

        return View(model);
    }
}
