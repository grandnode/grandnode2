using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.Filters;
using Grand.Web.Store.Extensions;
using Grand.Web.Store.Filters;
using Microsoft.AspNetCore.Mvc;
using Grand.Web.Common.Localization;

namespace Grand.Web.Store.Controllers;

/// <summary>
///     Lets a store manager manage the registered customers that belong to his own store. See
///     BaseCustomerController for the shared surface; the two members below are the genuine
///     Store-only pieces that don't belong in AdminShared: the page shown when the per-store feature
///     is off ([PerStoreCustomers] routes every action there), and the constraint-forcing override of
///     BaseCustomerController.ApplyPostConstraints.
/// </summary>
[AuthorizeStore]
[Area(Constants.AreaStore)]
[AuthorizeMenu]
[PerStoreCustomers]
public class CustomerController(
    Grand.Business.Core.Interfaces.Customers.ICustomerService customerService,
    ICustomerViewModelService customerViewModelService,
    Grand.Business.Core.Interfaces.Customers.ICustomerManagerService customerManagerService,
    Grand.Business.Core.Interfaces.Customers.ICustomerAttributeParser customerAttributeParser,
    Grand.Business.Core.Interfaces.Customers.ICustomerAttributeService customerAttributeService,
    Grand.Business.Core.Interfaces.Messages.ICustomerMessageProviderService customerMessageProviderService,
    IGroupService groupService,
    Grand.Web.Common.Localization.IRequestTranslationService translationService,
    IContextAccessor contextAccessor,
    CustomerSettings customerSettings,
    IAdminDataScope<Customer> scope)
    : BaseCustomerController(customerService, customerViewModelService, customerManagerService,
        customerAttributeParser, customerAttributeService, customerMessageProviderService, groupService,
        translationService, contextAccessor, customerSettings, scope)
{
    /// <summary>Shown instead of the panel when per-store customer identity is disabled.</summary>
    public IActionResult PerStoreDisabled() => View();

    /// <summary>Forces the store-scoped, registered-only constraints on the posted model
    /// regardless of what was sent, so the shared insert/update path in BaseCustomerController can
    /// never assign a foreign store, role, or ownership. See CustomerControllerTests's
    /// ApplyPostConstraints_CraftedPost_CannotSmuggleOwnershipFields for the exact contract.</summary>
    protected override async Task ApplyPostConstraints(CustomerModel model)
    {
        model.StoreId = Scope.DefaultStoreId;

        //where a customer belongs - owner, vendor, staff store, sales employee, groups - is not the store
        //manager's to change. An existing customer keeps what it has (blanking these used to strip them on every
        //save: a sub-account lost its owner, a customer its sales employee and its price groups, a colleague
        //the Store manager role); the posted values are never used either way
        var existing = string.IsNullOrEmpty(model.Id) ? null : await CustomerService.GetCustomerById(model.Id);
        if (existing != null)
        {
            var owner = string.IsNullOrEmpty(existing.OwnerId)
                ? null
                : await CustomerService.GetCustomerById(existing.OwnerId);
            model.Owner = owner?.Email ?? "";
            model.VendorId = existing.VendorId ?? "";
            model.StaffStoreId = existing.StaffStoreId ?? "";
            model.SeId = existing.SeId ?? "";
            model.CustomerGroups = existing.Groups.ToArray();
            return;
        }

        //a new customer belongs nowhere and is registered only
        model.Owner = "";
        model.VendorId = "";
        model.StaffStoreId = "";
        model.SeId = "";
        var registered = await GroupService.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Registered);
        model.CustomerGroups = registered != null ? new[] { registered.Id } : Array.Empty<string>();
    }
}
