#nullable enable

using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Interfaces.Marketing.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

/// <summary>
///     Admin-only Customer actions no other host has: Impersonate, RemoveAffiliate, DeleteSelected,
///     Export (All/Selected), CustomerNote (Select/Add/Delete), ContactFormList — plus the
///     TwoFactorEnabled warning on Create/Edit. Only Admin's concrete CustomerController inherits
///     this; Store's inherits <see cref="BaseCustomerController" /> directly (a genuine subset, same
///     shape as ARCH-001 Order's Vendor).
/// </summary>
public abstract class BaseCustomerManagementController(
    ICustomerService customerService,
    ICustomerViewModelService customerViewModelService,
    ICustomerManagerService customerManagerService,
    ICustomerProductService customerProductService,
    IProductReviewService productReviewService,
    IProductReviewViewModelService productReviewViewModelService,
    IProductViewModelService productViewModelService,
    ICustomerAttributeParser customerAttributeParser,
    ICustomerAttributeService customerAttributeService,
    IAddressAttributeParser addressAttributeParser,
    IAddressAttributeService addressAttributeService,
    IMessageProviderService messageProviderService,
    IGroupService groupService,
    ITranslationService translationService,
    IContextAccessor contextAccessor,
    CustomerSettings customerSettings,
    IAdminDataScope<Customer> scope,
    IPermissionService permissionService,
    IExportManager<Customer> exportManager)
    : BaseCustomerController(customerService, customerViewModelService, customerManagerService,
        customerProductService, productReviewService, productReviewViewModelService, productViewModelService,
        customerAttributeParser, customerAttributeService, addressAttributeParser, addressAttributeService,
        messageProviderService, groupService, translationService, contextAccessor, customerSettings, scope)
{
    protected override void CheckTwoFactorEnabledWarning(Customer? existingCustomer, CustomerModel model)
    {
        var wasEnabled = existingCustomer is not null &&
            existingCustomer.GetUserFieldFromEntity<bool>(SystemCustomerFieldNames.TwoFactorEnabled);
        if (!wasEnabled && model.TwoFactorEnabled)
            Warning(TranslationService.GetResource("Admin.Customers.Customers.CannotTwoFactorEnabled"));
    }

    #region Message contact form

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ContactFormList(DataSourceRequest command, string customerId)
    {
        var (contactFormModels, totalCount) =
            await CustomerViewModelService.PrepareContactFormModel(customerId, command.Page, command.PageSize);
        var gridModel = new DataSourceResult { Data = contactFormModels.ToList(), Total = totalCount };
        return Json(gridModel);
    }

    #endregion

    #region Impersonate / affiliate / bulk delete

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> Impersonate(string id)
    {
        var (customer, denied) = await LoadAuthorizedCustomer(id);
        if (customer is null) return denied!;

        if (!await permissionService.Authorize(StandardPermission.AllowCustomerImpersonation))
        {
            Error("User does not have permission for the impersonate session");
            return RedirectToAction("Edit", new { id = customer.Id });
        }

        if (!await GroupService.IsAdmin(ContextAccessor.WorkContext.CurrentCustomer) && await GroupService.IsAdmin(customer))
        {
            Error("A non-admin user cannot impersonate as an administrator");
            return RedirectToAction("Edit", new { id = customer.Id });
        }

        await CustomerService.UpdateUserField(ContextAccessor.WorkContext.CurrentCustomer,
            SystemCustomerFieldNames.ImpersonatedCustomerId, customer.Id);
        return RedirectToAction("Index", "Home", new { area = "" });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> RemoveAffiliate(string id)
    {
        var (customer, denied) = await LoadAuthorizedCustomer(id);
        if (customer is null) return denied!;

        customer.AffiliateId = "";
        await CustomerService.UpdateCustomerField(customer.Id, x => x.AffiliateId, customer.AffiliateId);
        return RedirectToAction("Edit", new { id = customer.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> DeleteSelected(ICollection<string> selectedIds)
    {
        if (selectedIds != null) await CustomerViewModelService.DeleteSelected(selectedIds.ToList());
        return Json(new { Result = true });
    }

    #endregion

    #region Customer note

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> CustomerNotesSelect(string customerId, DataSourceRequest command)
    {
        var (_, denied) = await LoadAuthorizedCustomer(customerId);
        if (denied != null) throw new ArgumentException("No customer found with the specified id");

        var customerNoteModels = await CustomerViewModelService.PrepareCustomerNoteList(customerId);
        var gridModel = new DataSourceResult { Data = customerNoteModels, Total = customerNoteModels.Count };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> CustomerNoteAdd(string customerId, string downloadId, bool displayToCustomer,
        string title, string message)
    {
        var (_, denied) = await LoadAuthorizedCustomer(customerId);
        if (denied != null) return Json(new { Result = false });

        await CustomerViewModelService.InsertCustomerNote(customerId, downloadId, displayToCustomer, title, message);
        return Json(new { Result = true });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> CustomerNoteDelete(string id, string customerId)
    {
        var (_, denied) = await LoadAuthorizedCustomer(customerId);
        if (denied != null) throw new ArgumentException("No customer found with the specified id");

        await CustomerViewModelService.DeleteCustomerNote(id, customerId);
        return new JsonResult("");
    }

    #endregion

    #region Export

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    [HttpPost]
    public async Task<IActionResult> ExportExcelAll(CustomerListModel model)
    {
        var salesEmployeeId = await GroupService.IsSalesManager(ContextAccessor.WorkContext.CurrentCustomer)
            ? ContextAccessor.WorkContext.CurrentCustomer.SeId
            : "";

        var customers = await CustomerService.GetAllCustomers(
            customerGroupIds: model.SearchCustomerGroupIds.ToArray(),
            salesEmployeeId: salesEmployeeId,
            email: model.SearchEmail,
            username: model.SearchUsername,
            firstName: model.SearchFirstName,
            lastName: model.SearchLastName,
            company: model.SearchCompany,
            phone: model.SearchPhone,
            zipPostalCode: model.SearchZipPostalCode,
            loadOnlyWithShoppingCart: false);

        try
        {
            var bytes = await exportManager.Export(customers);
            return File(bytes, "text/xls", "customers.xlsx");
        }
        catch (Exception exc)
        {
            Error(exc);
            return RedirectToAction("List");
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    [HttpPost]
    public async Task<IActionResult> ExportExcelSelected(string selectedIds)
    {
        var customers = new List<Customer>();
        if (selectedIds != null)
        {
            var ids = selectedIds.Split([','], StringSplitOptions.RemoveEmptyEntries).ToArray();
            customers.AddRange(await CustomerService.GetCustomersByIds(ids));
        }

        var bytes = await exportManager.Export(customers);
        return File(bytes, "text/xls", "customers.xlsx");
    }

    #endregion
}
