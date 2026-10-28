#nullable enable

using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Audit;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Controllers;

public abstract class BaseCustomerManagementController(
    ICustomerService customerService,
    ICustomerViewModelService customerViewModelService,
    ICustomerManagerService customerManagerService,
    ICustomerAttributeParser customerAttributeParser,
    ICustomerAttributeService customerAttributeService,
    ICustomerMessageProviderService customerMessageProviderService,
    IGroupService groupService,
    IRequestTranslationService translationService,
    IContextAccessor contextAccessor,
    CustomerSettings customerSettings,
    IAdminDataScope<Customer> scope,
    IRequestPermissionService permissionService,
    IExportManager<Customer> exportManager)
    : BaseCustomerController(customerService, customerViewModelService, customerManagerService,
        customerAttributeParser, customerAttributeService, customerMessageProviderService, groupService,
        translationService, contextAccessor, customerSettings, scope)
{
    protected override void CheckTwoFactorEnabledWarning(Customer? existingCustomer, CustomerModel model)
    {
        var wasEnabled = existingCustomer is not null &&
            existingCustomer.GetUserFieldFromEntity<bool>(SystemCustomerFieldNames.TwoFactorEnabled);
        if (!wasEnabled && model.TwoFactorEnabled)
            Warning(TranslationService.GetResource("Admin.Customers.Customers.CannotTwoFactorEnabled"));
    }

    #region Impersonate / affiliate / bulk delete

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> Impersonate(string id, [FromServices] IAuditEventRecorder auditEventRecorder)
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
        await auditEventRecorder.ImpersonationStart(customer);
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

    #region Export

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    [HttpPost]
    [AuditExport("Customer")]
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
            var bytes = await exportManager.Export(customers, HttpContext.RequestAborted);
            return File(bytes, "text/xls", "customers.xlsx");
        }
        catch (Exception exc) when (!IsRequestAborted(exc))
        {
            Error(exc);
            return RedirectToAction("List");
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    [HttpPost]
    [AuditExport("Customer")]
    public async Task<IActionResult> ExportExcelSelected(string selectedIds)
    {
        var customers = new List<Customer>();
        if (selectedIds != null)
        {
            var ids = selectedIds.Split([','], StringSplitOptions.RemoveEmptyEntries).ToArray();
            customers.AddRange(await CustomerService.GetCustomersByIds(ids));
        }

        var bytes = await exportManager.Export(customers, HttpContext.RequestAborted);
        return File(bytes, "text/xls", "customers.xlsx");
    }

    #endregion
}
