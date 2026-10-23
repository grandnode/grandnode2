#nullable enable

using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Models;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Controllers;

/// <summary>
///     The customer list, the customer form and the actions on its Info tab. Every other edit tab has
///     its own controller (BaseCustomerAddressController, BaseCustomerOrderController, ...).
/// </summary>
public abstract class BaseCustomerController(
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
    IAdminDataScope<Customer> scope)
    : BaseCustomerScopedController(customerService, scope)
{
    // Exposed for BaseCustomerManagementController (primary-constructor parameters aren't visible
    // to derived classes by name in C#).
    protected ICustomerViewModelService CustomerViewModelService => customerViewModelService;
    protected IGroupService GroupService => groupService;
    protected IRequestTranslationService TranslationService => translationService;
    protected IContextAccessor ContextAccessor => contextAccessor;

    /// <summary>Store-only: forces StoreId/blanks Owner/VendorId/StaffStoreId/SeId/CustomerGroups
    /// on the posted model, ported from Store's original ApplyStoreConstraints. No-op for Admin
    /// (BaseCustomerManagementController never overrides this — Admin never had this method).</summary>
    protected virtual Task ApplyPostConstraints(CustomerModel model) => Task.CompletedTask;

    private void ClearAvailableStoresForStoreScope(CustomerModel model)
    {
        if (Scope.DefaultStoreId is not null)
            model.AvailableStores.Clear();
    }

    /// <summary>Admin-only: warns when a submitted model newly enables two-factor auth. No-op here
    /// — Store's originals never referenced TwoFactorEnabled at all. existingCustomer is null on
    /// Create, which collapses to Admin's original simpler create-time condition automatically.</summary>
    protected virtual void CheckTwoFactorEnabledWarning(Customer? existingCustomer, CustomerModel model)
    {
    }

    protected virtual async Task<IList<CustomAttribute>> ParseCustomCustomerAttributes(
        IList<CustomAttributeModel> model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var customAttributes = new List<CustomAttribute>();
        var customerAttributes = await customerAttributeService.GetAllCustomerAttributes();
        foreach (var attribute in customerAttributes)
        {
            switch (attribute.AttributeControlTypeId)
            {
                case AttributeControlType.DropdownList:
                case AttributeControlType.RadioList:
                    {
                        var ctrlAttributes = model.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                        if (!string.IsNullOrEmpty(ctrlAttributes))
                            customAttributes = customerAttributeParser
                                .AddCustomerAttribute(customAttributes, attribute, ctrlAttributes).ToList();
                    }
                    break;
                case AttributeControlType.Checkboxes:
                    {
                        var cblAttributes = model.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                        if (!string.IsNullOrEmpty(cblAttributes))
                            foreach (var item in cblAttributes.Split(',').Where(x => !string.IsNullOrEmpty(x)))
                                customAttributes = customerAttributeParser
                                    .AddCustomerAttribute(customAttributes, attribute, item).ToList();
                    }
                    break;
                case AttributeControlType.ReadonlyCheckboxes:
                    {
                        foreach (var selectedAttributeId in attribute.CustomerAttributeValues
                                     .Where(v => v.IsPreSelected).Select(v => v.Id).ToList())
                            customAttributes = customerAttributeParser
                                .AddCustomerAttribute(customAttributes, attribute, selectedAttributeId).ToList();
                    }
                    break;
                case AttributeControlType.TextBox:
                case AttributeControlType.MultilineTextbox:
                    {
                        var ctrlAttributes = model.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                        if (!string.IsNullOrEmpty(ctrlAttributes))
                            customAttributes = customerAttributeParser
                                .AddCustomerAttribute(customAttributes, attribute, ctrlAttributes.Trim()).ToList();
                    }
                    break;
                default:
                    break;
            }
        }

        return customAttributes;
    }

    #region Customers

    public IActionResult Index() => RedirectToAction("List");

    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> List()
    {
        // Fix: unconditionally call the shared, fully-populated PrepareCustomerListModel() for
        // BOTH hosts. Store's original bypassed this (`new CustomerListModel()`), silently leaving
        // UsernamesEnabled/CompanyEnabled/PhoneEnabled/ZipPostalCodeEnabled false regardless of
        // CustomerSettings, hiding those 4 optional grid columns even when configured on. Store's
        // view never reads AvailableCustomerGroups/AvailableCustomerTags, so computing them for
        // Store too is harmless, not a correctness requirement — see spec "List() model gap".
        var model = await customerViewModelService.PrepareCustomerListModel();
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> CustomerList(DataSourceRequest command, CustomerListModel model,
        string[] searchCustomerGroupIds, string[] searchCustomerTagIds)
    {
        var groupIds = searchCustomerGroupIds;
        string[]? tagIds = searchCustomerTagIds;
        if (Scope.DefaultStoreId is not null)
        {
            // Store panel always restricts to the Registered group regardless of what was
            // submitted — ports Store's original hardcoded registered-only CustomerList.
            var registered = await groupService.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Registered);
            groupIds = registered != null ? new[] { registered.Id } : Array.Empty<string>();
            tagIds = null;
        }

        var (customerModelList, totalCount) = await customerViewModelService.PrepareCustomerList(model,
            groupIds, tagIds, command.Page, command.PageSize, Scope.DefaultStoreId ?? "");
        var gridModel = new DataSourceResult {
            Data = customerModelList.ToList(),
            Total = totalCount
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        var model = new CustomerModel();
        await customerViewModelService.PrepareCustomerModel(model, null, false);
        await ApplyPostConstraints(model);
        ClearAvailableStoresForStoreScope(model);
        model.Active = true;
        return View(model);
    }

    // Disclosed bug fix, same class as GiftVoucher's: Admin's pre-consolidation Create(POST)
    // required PermissionActionName.Edit while everything else on Create required .Create — Store's
    // original already used .Create. Harmonized on .Create here (matches every other Base*Controller
    // in AdminShared). See CustomerControllerAttributeTests.CreatePost_RequiresCreatePermission.
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Create(CustomerModel model, bool continueEditing)
    {
        await ApplyPostConstraints(model);
        CheckTwoFactorEnabledWarning(null, model);

        if (ModelState.IsValid)
        {
            model.Attributes = await ParseCustomCustomerAttributes(model.SelectedAttributes);
            var customer = await customerViewModelService.InsertCustomerModel(model);

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var changePassRequest = new ChangePasswordRequest(model.Email,
                    customerSettings.DefaultPasswordFormat, model.Password);
                await customerManagerService.ChangePassword(changePassRequest, customer.StoreId);
            }

            Success(translationService.GetResource("Admin.Customers.Customers.Added"));
            return continueEditing ? RedirectToAction("Edit", new { id = customer.Id }) : RedirectToAction("List");
        }

        await customerViewModelService.PrepareCustomerModel(model, null, true);
        await ApplyPostConstraints(model);
        ClearAvailableStoresForStoreScope(model);
        return View(model);
    }

    #endregion

    #region Edit / Delete

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var (customer, denied) = await LoadAuthorizedCustomer(id);
        if (customer is null) return denied!;

        var model = new CustomerModel();
        await customerViewModelService.PrepareCustomerModel(model, customer, false);
        ClearAvailableStoresForStoreScope(model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(CustomerModel model, bool continueEditing)
    {
        var (customer, denied) = await LoadAuthorizedCustomer(model.Id);
        if (customer is null) return denied!;

        await ApplyPostConstraints(model);
        CheckTwoFactorEnabledWarning(customer, model);

        if (ModelState.IsValid)
            try
            {
                model.Attributes = await ParseCustomCustomerAttributes(model.SelectedAttributes);
                customer = await customerViewModelService.UpdateCustomerModel(customer, model);

                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    var changePassRequest = new ChangePasswordRequest(model.Email,
                        customerSettings.DefaultPasswordFormat, model.Password);
                    await customerManagerService.ChangePassword(changePassRequest, customer.StoreId);
                }

                Success(translationService.GetResource("Admin.Customers.Customers.Updated"));
                if (continueEditing)
                {
                    await SaveSelectedTabIndex();
                    return RedirectToAction("Edit", new { id = customer.Id });
                }

                return RedirectToAction("List");
            }
            catch (Exception exc)
            {
                Error(exc.Message);
            }

        await customerViewModelService.PrepareCustomerModel(model, customer, true);
        await ApplyPostConstraints(model);
        ClearAvailableStoresForStoreScope(model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var (customer, denied) = await LoadAuthorizedCustomer(id);
        if (customer is null) return denied!;

        if (customer.Id == contextAccessor.WorkContext.CurrentCustomer.Id)
        {
            Error(translationService.GetResource("Admin.Customers.Customers.NoSelfDelete"));
            return RedirectToAction("List");
        }

        try
        {
            if (ModelState.IsValid)
            {
                await customerViewModelService.DeleteCustomer(customer);
                Success(translationService.GetResource("Admin.Customers.Customers.Deleted"));
                return RedirectToAction("List");
            }

            Error(ModelState);
            return RedirectToAction("Edit", new { id = customer.Id });
        }
        catch (Exception exc)
        {
            Error(exc.Message);
            return RedirectToAction("Edit", new { id = customer.Id });
        }
    }

    #endregion

    #region Vat / Messages

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> MarkVatNumberAsValid(string id)
    {
        var (customer, denied) = await LoadAuthorizedCustomer(id);
        if (customer is null) return denied!;

        await CustomerService.UpdateUserField(customer, SystemCustomerFieldNames.VatNumberStatusId,
            (int)Grand.Domain.Tax.VatNumberStatus.Valid);
        return RedirectToAction("Edit", new { id = customer.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> MarkVatNumberAsInvalid(string id)
    {
        var (customer, denied) = await LoadAuthorizedCustomer(id);
        if (customer is null) return denied!;

        await CustomerService.UpdateUserField(customer, SystemCustomerFieldNames.VatNumberStatusId,
            (int)Grand.Domain.Tax.VatNumberStatus.Invalid);
        return RedirectToAction("Edit", new { id = customer.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SendWelcomeMessage(string id)
    {
        var (customer, denied) = await LoadAuthorizedCustomer(id);
        if (customer is null) return denied!;

        await customerMessageProviderService.SendCustomerWelcomeMessage(customer, contextAccessor.StoreContext.CurrentStore,
            contextAccessor.WorkContext.WorkingLanguage.Id);
        Success(translationService.GetResource("Admin.Customers.Customers.SendWelcomeMessage.Success"));
        return RedirectToAction("Edit", new { id = customer.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ReSendActivationMessage(string id)
    {
        var (customer, denied) = await LoadAuthorizedCustomer(id);
        if (customer is null) return denied!;

        await CustomerService.UpdateUserField(customer, SystemCustomerFieldNames.AccountActivationToken,
            Guid.NewGuid().ToString());
        await customerMessageProviderService.SendCustomerEmailValidationMessage(customer,
            contextAccessor.StoreContext.CurrentStore, contextAccessor.WorkContext.WorkingLanguage.Id);
        Success(translationService.GetResource("Admin.Customers.Customers.ReSendActivationMessage.Success"));
        return RedirectToAction("Edit", new { id = customer.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> SendEmail(CustomerModel.SendEmailModel model)
    {
        var (customer, denied) = await LoadAuthorizedCustomer(model.Id);
        if (customer is null) return denied!;

        try
        {
            if (string.IsNullOrWhiteSpace(customer.Email))
                throw new Grand.SharedKernel.GrandException("Customer email is empty");
            if (!Grand.SharedKernel.Extensions.CommonHelper.IsValidEmail(customer.Email))
                throw new Grand.SharedKernel.GrandException("Customer email is not valid");
            if (string.IsNullOrWhiteSpace(model.Subject))
                throw new Grand.SharedKernel.GrandException("Email subject is empty");
            if (string.IsNullOrWhiteSpace(model.Body))
                throw new Grand.SharedKernel.GrandException("Email body is empty");

            await customerViewModelService.SendEmail(customer, model);
            Success(translationService.GetResource("Admin.Customers.Customers.SendEmail.Queued"));
        }
        catch (Exception exc)
        {
            Error(exc.Message);
        }

        return RedirectToAction("Edit", new { id = customer.Id });
    }

    #endregion

    #region External authentication

    /// <summary>
    ///     Removes a link to an external sign-in (e.g. after the person was deleted and created again at the identity
    ///     provider); the next provider sign-in connects the account again by the rules of a first sign-in
    /// </summary>
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ExternalAuthenticationDelete(string id, string customerId,
        [FromServices] IOidcLinkService linkService)
    {
        var (customer, _) = await LoadAuthorizedCustomer(customerId);
        if (customer is null) throw new ArgumentException("No customer found with the specified id", nameof(customerId));

        if (!await linkService.Remove(customer, id))
            return ErrorForKendoGridJson("No external sign-in found with the specified id");
        return new JsonResult("");
    }

    #endregion
}
