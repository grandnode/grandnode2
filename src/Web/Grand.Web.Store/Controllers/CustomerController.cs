using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.Filters;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Grand.Web.Store.Controllers;

/// <summary>
///     Lets a store manager manage the registered customers that belong to his own store. See
///     BaseCustomerController for the shared surface; the three members below are the genuine
///     Store-only pieces that don't belong in AdminShared: the per-store feature gate, the page
///     shown when that feature is off, and the constraint-forcing override of
///     BaseCustomerController.ApplyPostConstraints.
/// </summary>
[AuthorizeStore]
[Area(Constants.AreaStore)]
[AuthorizeMenu]
public class CustomerController(
    Grand.Business.Core.Interfaces.Customers.ICustomerService customerService,
    ICustomerViewModelService customerViewModelService,
    Grand.Business.Core.Interfaces.Customers.ICustomerManagerService customerManagerService,
    Grand.Business.Core.Interfaces.Marketing.Customers.ICustomerProductService customerProductService,
    Grand.Business.Core.Interfaces.Catalog.Products.IProductReviewService productReviewService,
    IProductReviewViewModelService productReviewViewModelService,
    IProductViewModelService productViewModelService,
    Grand.Business.Core.Interfaces.Customers.ICustomerAttributeParser customerAttributeParser,
    Grand.Business.Core.Interfaces.Customers.ICustomerAttributeService customerAttributeService,
    Grand.Business.Core.Interfaces.Common.Addresses.IAddressAttributeParser addressAttributeParser,
    Grand.Business.Core.Interfaces.Common.Addresses.IAddressAttributeService addressAttributeService,
    Grand.Business.Core.Interfaces.Messages.IMessageProviderService messageProviderService,
    IGroupService groupService,
    Grand.Business.Core.Interfaces.Common.Localization.ITranslationService translationService,
    IContextAccessor contextAccessor,
    CustomerSettings customerSettings,
    IAdminDataScope<Customer> scope,
    CustomerConfig customerConfig)
    : BaseCustomerController(customerService, customerViewModelService, customerManagerService,
        customerProductService, productReviewService, productReviewViewModelService, productViewModelService,
        customerAttributeParser, customerAttributeService, addressAttributeParser, addressAttributeService,
        messageProviderService, groupService, translationService, contextAccessor, customerSettings, scope)
{
    //managing customers from the store panel only makes sense when customer identity is scoped per
    //store (Customer:RegisterCustomersPerStore). When it's off the whole controller is disabled and
    //every action is routed to the PerStoreDisabled page that explains how to enable the setting.
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!customerConfig.RegisterCustomersPerStore &&
            context.ActionDescriptor is ControllerActionDescriptor { ActionName: not nameof(PerStoreDisabled) })
        {
            context.Result = RedirectToAction(nameof(PerStoreDisabled));
            return;
        }

        await next();
    }

    /// <summary>Shown instead of the panel when per-store customer identity is disabled.</summary>
    public IActionResult PerStoreDisabled() => View();

    /// <summary>Forces the store-scoped, registered-only constraints on the posted model
    /// regardless of what was sent, so the shared insert/update path in BaseCustomerController can
    /// never assign a foreign store, role, or ownership. See CustomerControllerTests's
    /// ApplyPostConstraints_CraftedPost_CannotSmuggleOwnershipFields for the exact contract.</summary>
    protected override async Task ApplyPostConstraints(CustomerModel model)
    {
        model.StoreId = scope.DefaultStoreId;
        model.Owner = "";
        model.VendorId = "";
        model.StaffStoreId = "";
        model.SeId = "";
        var registered = await groupService.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Registered);
        model.CustomerGroups = registered != null ? new[] { registered.Id } : Array.Empty<string>();
    }
}
