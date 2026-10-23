#nullable enable

using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

/// <summary>
///     Shared base of the customer controller and its edit tab controllers: every one of them acts on
///     a single customer, which must exist and lie inside the panel's data scope.
/// </summary>
[PermissionAuthorize(PermissionSystemName.Customers)]
[AutoValidateAntiforgeryToken]
public abstract class BaseCustomerScopedController(
    ICustomerService customerService,
    IAdminDataScope<Customer> scope)
    : BaseController
{
    protected ICustomerService CustomerService => customerService;
    protected IAdminDataScope<Customer> Scope => scope;

    /// <summary>Loads the customer and checks the scope. Both a missing and an out-of-scope customer
    /// redirect to the customer list, whichever controller the action lives on.</summary>
    protected async Task<(Customer? customer, IActionResult? denied)> LoadAuthorizedCustomer(string id)
    {
        var customer = await customerService.GetCustomerById(id);
        if (customer == null) return (null, RedirectToAction("List", "Customer"));
        if (!await scope.HasAccess(customer)) return (null, RedirectToAction("List", "Customer"));
        return (customer, null);
    }

    /// <summary>Non-redirecting ownership check for actions that soft-deny with an empty JSON
    /// result instead of a redirect (e.g. UpdateProductPrice/DeleteProductPrice).</summary>
    protected async Task<bool> HasAccessToCustomer(string customerId)
    {
        var customer = await customerService.GetCustomerById(customerId);
        return customer != null && await scope.HasAccess(customer);
    }
}
