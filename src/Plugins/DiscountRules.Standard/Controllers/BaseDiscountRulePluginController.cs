using Grand.Domain.Customers;
using Grand.Domain.Discounts;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;

namespace DiscountRules.Standard.Controllers;

[AuthorizeAdminOrStore]
[AutoValidateAntiforgeryToken]
public abstract class BaseDiscountRulePluginController : BaseController
{
    //staff of a store configure only a discount of their store alone, as on the store panel's discount screen
    protected static bool CanManage(Discount discount, Customer customer) =>
        string.IsNullOrEmpty(customer.StaffStoreId) ||
        (discount.LimitedToStores && discount.Stores.Count == 1 && discount.Stores[0] == customer.StaffStoreId);
}
