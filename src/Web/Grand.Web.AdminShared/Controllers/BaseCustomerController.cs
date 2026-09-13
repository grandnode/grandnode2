#nullable enable

using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Marketing.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.AdminShared.Extensions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Models;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.Customers)]
[AutoValidateAntiforgeryToken]
public abstract class BaseCustomerController(
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
    IAdminDataScope<Customer> scope)
    : BaseController
{
    // Exposed for BaseCustomerManagementController (primary-constructor parameters aren't visible
    // to derived classes by name in C#).
    protected ICustomerService CustomerService => customerService;
    protected ICustomerViewModelService CustomerViewModelService => customerViewModelService;
    protected ICustomerManagerService CustomerManagerService => customerManagerService;
    protected ICustomerProductService CustomerProductService => customerProductService;
    protected IProductReviewService ProductReviewService => productReviewService;
    protected IProductReviewViewModelService ProductReviewViewModelService => productReviewViewModelService;
    protected IProductViewModelService ProductViewModelService => productViewModelService;
    protected ICustomerAttributeParser CustomerAttributeParser => customerAttributeParser;
    protected ICustomerAttributeService CustomerAttributeService => customerAttributeService;
    protected IAddressAttributeParser AddressAttributeParser => addressAttributeParser;
    protected IAddressAttributeService AddressAttributeService => addressAttributeService;
    protected IMessageProviderService MessageProviderService => messageProviderService;
    protected IGroupService GroupService => groupService;
    protected ITranslationService TranslationService => translationService;
    protected IContextAccessor ContextAccessor => contextAccessor;
    protected CustomerSettings CustomerSettings => customerSettings;
    protected IAdminDataScope<Customer> Scope => scope;

    /// <summary>Store-only: forces StoreId/blanks Owner/VendorId/StaffStoreId/SeId/CustomerGroups
    /// on the posted model, ported from Store's original ApplyStoreConstraints. No-op for Admin
    /// (BaseCustomerManagementController never overrides this — Admin never had this method).</summary>
    protected virtual Task ApplyPostConstraints(CustomerModel model) => Task.CompletedTask;

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
        if (scope.DefaultStoreId is not null)
        {
            // Store panel always restricts to the Registered group regardless of what was
            // submitted — ports Store's original hardcoded registered-only CustomerList.
            var registered = await groupService.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Registered);
            groupIds = registered != null ? new[] { registered.Id } : Array.Empty<string>();
            tagIds = null;
        }

        var (customerModelList, totalCount) = await customerViewModelService.PrepareCustomerList(model,
            groupIds, tagIds, command.Page, command.PageSize, scope.DefaultStoreId ?? "");
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
        return View(model);
    }

    #endregion

    /// <summary>DRY replacement for the repeated "load customer, redirect to List if not found or
    /// not authorized" pattern in both original controllers — every action in both files redirects
    /// to "List", never "Edit", on either condition.</summary>
    protected async Task<(Customer? customer, IActionResult? denied)> LoadAuthorizedCustomer(string id)
    {
        var customer = await customerService.GetCustomerById(id);
        if (customer == null) return (null, RedirectToAction("List"));
        if (!await scope.HasAccess(customer)) return (null, RedirectToAction("List"));
        return (customer, null);
    }

    #region Edit / Delete

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var (customer, denied) = await LoadAuthorizedCustomer(id);
        if (customer is null) return denied!;

        var model = new CustomerModel();
        await customerViewModelService.PrepareCustomerModel(model, customer, false);
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

        await customerService.UpdateUserField(customer, SystemCustomerFieldNames.VatNumberStatusId,
            (int)Grand.Domain.Tax.VatNumberStatus.Valid);
        return RedirectToAction("Edit", new { id = customer.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> MarkVatNumberAsInvalid(string id)
    {
        var (customer, denied) = await LoadAuthorizedCustomer(id);
        if (customer is null) return denied!;

        await customerService.UpdateUserField(customer, SystemCustomerFieldNames.VatNumberStatusId,
            (int)Grand.Domain.Tax.VatNumberStatus.Invalid);
        return RedirectToAction("Edit", new { id = customer.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SendWelcomeMessage(string id)
    {
        var (customer, denied) = await LoadAuthorizedCustomer(id);
        if (customer is null) return denied!;

        await messageProviderService.SendCustomerWelcomeMessage(customer, contextAccessor.StoreContext.CurrentStore,
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

        await customerService.UpdateUserField(customer, SystemCustomerFieldNames.AccountActivationToken,
            Guid.NewGuid().ToString());
        await messageProviderService.SendCustomerEmailValidationMessage(customer,
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

    #region Loyalty points history

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> LoyaltyPointsHistorySelect(string customerId)
    {
        var (customer, denied) = await LoadAuthorizedCustomer(customerId);
        if (customer is null) throw new ArgumentException("No customer found with the specified id");

        var model = (await customerViewModelService.PrepareLoyaltyPointsHistoryModel(customerId)).ToList();
        var gridModel = new DataSourceResult { Data = model, Total = model.Count };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> LoyaltyPointsHistoryAdd(string customerId, string storeId,
        int addLoyaltyPointsValue, string addLoyaltyPointsMessage)
    {
        var (customer, denied) = await LoadAuthorizedCustomer(customerId);
        if (customer is null) return Json(new { Result = false });

        // Store scope always forces its own StaffStoreId, ignoring the caller-supplied storeId —
        // ports the original controller's `CurrentStoreId` usage. Admin has no store concept here
        // and keeps using whatever the caller (its own store picker) submitted.
        var effectiveStoreId = scope.DefaultStoreId ?? storeId;

        await customerViewModelService.InsertLoyaltyPointsHistory(customer, effectiveStoreId,
            addLoyaltyPointsValue, addLoyaltyPointsMessage);
        return Json(new { Result = true });
    }

    #endregion

    #region Addresses

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> AddressesSelect(string customerId, DataSourceRequest command)
    {
        var (customer, denied) = await LoadAuthorizedCustomer(customerId);
        if (customer is null) throw new ArgumentException("No customer found with the specified id", nameof(customerId));

        var addresses = (await customerViewModelService.PrepareAddressModel(customer)).ToList();
        var gridModel = new DataSourceResult { Data = addresses, Total = addresses.Count };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AddressDelete(string id, string customerId)
    {
        var (customer, denied) = await LoadAuthorizedCustomer(customerId);
        if (customer is null) throw new ArgumentException("No customer found with the specified id", nameof(customerId));

        var address = customer.Addresses.FirstOrDefault(a => a.Id == id);
        if (address == null) return Content("No customer found with the specified id");
        if (ModelState.IsValid)
        {
            await customerViewModelService.DeleteAddress(customer, address);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> AddressCreate(string customerId)
    {
        var (customer, denied) = await LoadAuthorizedCustomer(customerId);
        if (customer is null) return denied!;

        var model = new CustomerAddressModel();
        await customerViewModelService.PrepareAddressModel(model, null, customer, false);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AddressCreate(CustomerAddressModel model)
    {
        var (customer, denied) = await LoadAuthorizedCustomer(model.CustomerId);
        if (customer is null) return denied!;

        if (ModelState.IsValid)
        {
            var customAttributes =
                await model.Address.ParseCustomAddressAttributes(addressAttributeParser, addressAttributeService);
            var address = await customerViewModelService.InsertAddressModel(customer, model, customAttributes);
            Success(translationService.GetResource("Admin.Customers.Customers.Addresses.Added"));
            return RedirectToAction("AddressEdit", new { addressId = address.Id, customerId = model.CustomerId });
        }

        await customerViewModelService.PrepareAddressModel(model, null, customer, true);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> AddressEdit(string addressId, string customerId)
    {
        var (customer, denied) = await LoadAuthorizedCustomer(customerId);
        if (customer is null) return denied!;

        var address = customer.Addresses.FirstOrDefault(x => x.Id == addressId);
        if (address == null) return RedirectToAction("Edit", new { id = customer.Id });

        var model = new CustomerAddressModel();
        await customerViewModelService.PrepareAddressModel(model, address, customer, false);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AddressEdit(CustomerAddressModel model)
    {
        var (customer, denied) = await LoadAuthorizedCustomer(model.CustomerId);
        if (customer is null) return denied!;

        var address = customer.Addresses.FirstOrDefault(x => x.Id == model.Address.Id);
        if (address == null) return RedirectToAction("Edit", new { id = customer.Id });

        if (ModelState.IsValid)
        {
            var customAttributes =
                await model.Address.ParseCustomAddressAttributes(addressAttributeParser, addressAttributeService);
            await customerViewModelService.UpdateAddressModel(customer, address, model, customAttributes);
            Success(translationService.GetResource("Admin.Customers.Customers.Addresses.Updated"));
            return RedirectToAction("AddressEdit", new { addressId = model.Address.Id, customerId = model.CustomerId });
        }

        await customerViewModelService.PrepareAddressModel(model, address, customer, true);
        return View(model);
    }

    #endregion

    #region Orders

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> OrderList(string customerId, DataSourceRequest command,
        [FromServices] Grand.Business.Core.Interfaces.Checkout.Orders.IOrderService orderService,
        [FromServices] Grand.Web.AdminShared.Interfaces.IOrderViewModelService orderViewModelService,
        [FromServices] IPermissionService permissionService)
    {
        if (scope.DefaultStoreId is not null)
        {
            var (_, denied) = await LoadAuthorizedCustomer(customerId);
            if (denied != null) return Json(new DataSourceResult { Data = null, Total = 0 });
        }
        else if (!await permissionService.Authorize(StandardPermission.ManageOrders))
        {
            return Json(new DataSourceResult { Data = null, Total = 0 });
        }

        var model = new Grand.Web.AdminShared.Models.Orders.OrderListModel {
            CustomerId = customerId, StoreId = scope.DefaultStoreId ?? ""
        };
        var (orderModels, totalCount) = await orderViewModelService.PrepareOrderModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult { Data = orderModels.ToList(), Total = totalCount };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> OrderDetails(string orderId,
        [FromServices] Grand.Business.Core.Interfaces.Checkout.Orders.IOrderService orderService,
        [FromServices] Grand.Web.AdminShared.Interfaces.IOrderViewModelService orderViewModelService,
        [FromServices] IPermissionService permissionService)
    {
        Grand.Domain.Orders.Order order;
        if (scope.DefaultStoreId is null)
        {
            if (!await permissionService.Authorize(StandardPermission.ManageOrders))
                return Json(new DataSourceResult { Data = null, Total = 0 });

            order = await orderService.GetOrderById(orderId);
            if (order == null) throw new ArgumentException("No order found with the specified id");
        }
        else
        {
            order = await orderService.GetOrderById(orderId);
            if (order == null || order.StoreId != scope.DefaultStoreId)
                return Json(new DataSourceResult { Data = null, Total = 0 });
        }

        var ordermodel = new Grand.Web.AdminShared.Models.Orders.OrderModel();
        await orderViewModelService.PrepareOrderDetailsModel(ordermodel, order);
        var gridModel = new DataSourceResult { Data = ordermodel.Items, Total = ordermodel.Items.Count };
        return Json(gridModel);
    }

    #endregion

    #region Reviews

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ReviewList(string customerId, DataSourceRequest command)
    {
        if (scope.DefaultStoreId is not null)
        {
            var (_, denied) = await LoadAuthorizedCustomer(customerId);
            if (denied != null) return Json(new DataSourceResult { Data = null, Total = 0 });
        }

        var productReviews = await productReviewService.GetAllProductReviews(customerId, null,
            null, null, "", scope.DefaultStoreId, "", command.Page - 1, command.PageSize);
        var items = new List<Grand.Web.AdminShared.Models.Catalog.ProductReviewModel>();
        foreach (var x in productReviews)
        {
            var m = new Grand.Web.AdminShared.Models.Catalog.ProductReviewModel();
            await productViewModelService.PrepareProductReviewModel(m, x, false, true);
            items.Add(m);
        }

        var gridModel = new DataSourceResult { Data = items, Total = productReviews.TotalCount };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ReviewDelete(string id)
    {
        var productReview = await productReviewService.GetProductReviewById(id);
        if (productReview == null) throw new ArgumentException("No review found with the specified id", nameof(id));
        if (scope.DefaultStoreId is not null && productReview.StoreId != scope.DefaultStoreId)
            throw new ArgumentException("No review found with the specified id", nameof(id));

        await productReviewViewModelService.DeleteProductReview(productReview);
        return new JsonResult("");
    }

    #endregion

    #region Current shopping cart / wishlist

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> GetCartList(string customerId, int cartTypeId)
    {
        // Admin's original never checked ownership here at all — preserved exactly, see task note.
        if (scope.DefaultStoreId is not null)
        {
            var (_, denied) = await LoadAuthorizedCustomer(customerId);
            if (denied != null) return Json(new DataSourceResult { Data = null, Total = 0 });
        }

        var cart = await customerViewModelService.PrepareShoppingCartItemModel(customerId, cartTypeId);
        var gridModel = new DataSourceResult { Data = cart, Total = cart.Count };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> UpdateCart(string id, string customerId, double? unitPriceValue)
    {
        var (customer, denied) = await LoadAuthorizedCustomer(customerId);
        if (customer is null) throw new ArgumentException("No customer found with the specified id", nameof(customerId));

        var warnings = await customerViewModelService.UpdateCart(customer, id, unitPriceValue);
        if (warnings.Any()) return ErrorForKendoGridJson(string.Join(",", warnings));
        return new JsonResult("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> DeleteCart(string id, string customerId)
    {
        var (customer, denied) = await LoadAuthorizedCustomer(customerId);
        if (customer is null) throw new ArgumentException("No customer found with the specified id", nameof(customerId));

        await customerViewModelService.DeleteCart(customer, id);
        return new JsonResult("");
    }

    #endregion

    /// <summary>Non-redirecting ownership check for actions that soft-deny with an empty JSON
    /// result instead of a redirect (e.g. UpdateProductPrice/DeleteProductPrice).</summary>
    protected async Task<bool> HasAccessToCustomer(string customerId)
    {
        var customer = await customerService.GetCustomerById(customerId);
        return customer != null && await scope.HasAccess(customer);
    }

    #region Customer Product Personalize / Price

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductsPrice(DataSourceRequest command, string customerId)
    {
        if (scope.DefaultStoreId is not null)
        {
            var (_, denied) = await LoadAuthorizedCustomer(customerId);
            if (denied != null) return Json(new DataSourceResult { Data = null, Total = 0 });
        }

        var (productPriceModels, totalCount) =
            await customerViewModelService.PrepareProductPriceModel(customerId, command.Page, command.PageSize);
        var gridModel = new DataSourceResult { Data = productPriceModels.ToList(), Total = totalCount };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> PersonalizedProducts(DataSourceRequest command, string customerId)
    {
        if (scope.DefaultStoreId is not null)
        {
            var (_, denied) = await LoadAuthorizedCustomer(customerId);
            if (denied != null) return Json(new DataSourceResult { Data = null, Total = 0 });
        }

        var (productModels, totalCount) =
            await customerViewModelService.PreparePersonalizedProducts(customerId, command.Page, command.PageSize);
        var gridModel = new DataSourceResult { Data = productModels.ToList(), Total = totalCount };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductAddPopup(string customerId)
    {
        if (scope.DefaultStoreId is not null)
        {
            var (_, denied) = await LoadAuthorizedCustomer(customerId);
            if (denied != null) return denied;
        }

        var model = await customerViewModelService.PrepareCustomerModelAddProductModel();
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAddPopupList(DataSourceRequest command, CustomerModel.AddProductModel model)
    {
        var products = await customerViewModelService.PrepareProductModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult { Data = products.products.ToList(), Total = products.totalCount };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAddPopup(string customerId, bool personalized,
        CustomerModel.AddProductModel model)
    {
        if (scope.DefaultStoreId is not null)
        {
            var (_, denied) = await LoadAuthorizedCustomer(customerId);
            if (denied != null) return Content("");
        }

        if (model.SelectedProductIds != null)
            await customerViewModelService.InsertCustomerAddProductModel(customerId, personalized, model);
        return Content("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> UpdateProductPrice(CustomerModel.ProductPriceModel model)
    {
        if (scope.DefaultStoreId is not null)
        {
            var productPrice = await customerProductService.GetCustomerProductPriceById(model.Id);
            if (productPrice == null || !await HasAccessToCustomer(productPrice.CustomerId))
                return new JsonResult("");
        }

        await customerViewModelService.UpdateProductPrice(model);
        return new JsonResult("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> DeleteProductPrice(string id)
    {
        if (scope.DefaultStoreId is not null)
        {
            var productPrice = await customerProductService.GetCustomerProductPriceById(id);
            if (productPrice == null || !await HasAccessToCustomer(productPrice.CustomerId))
                return new JsonResult("");

            await customerProductService.DeleteCustomerProductPrice(productPrice);
            return new JsonResult("");
        }

        await customerViewModelService.DeleteProductPrice(id);
        return new JsonResult("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> UpdatePersonalizedProduct(CustomerModel.ProductModel model)
    {
        if (scope.DefaultStoreId is not null)
        {
            var customerProduct = await customerProductService.GetCustomerProduct(model.Id);
            if (customerProduct == null || !await HasAccessToCustomer(customerProduct.CustomerId))
                return new JsonResult("");
        }

        await customerViewModelService.UpdatePersonalizedProduct(model);
        return new JsonResult("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> DeletePersonalizedProduct(string id)
    {
        if (scope.DefaultStoreId is not null)
        {
            var customerProduct = await customerProductService.GetCustomerProduct(id);
            if (customerProduct == null || !await HasAccessToCustomer(customerProduct.CustomerId))
                return new JsonResult("");

            await customerProductService.DeleteCustomerProduct(customerProduct);
            return new JsonResult("");
        }

        // Admin's original convenience method, preserved for the global-scope branch (still present
        // on ICustomerViewModelService — no need to fall back to a two-step Get+Delete).
        await customerViewModelService.DeletePersonalizedProduct(id);
        return new JsonResult("");
    }

    #endregion

    #region Out of stock subscriptions

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> OutOfStockSubscriptionList(DataSourceRequest command, string customerId)
    {
        if (scope.DefaultStoreId is not null)
        {
            var (_, denied) = await LoadAuthorizedCustomer(customerId);
            if (denied != null) return Json(new DataSourceResult { Data = null, Total = 0 });
        }

        var (outOfStockSubscriptionModels, totalCount) =
            await customerViewModelService.PrepareOutOfStockSubscriptionModel(customerId, command.Page, command.PageSize);
        var gridModel = new DataSourceResult { Data = outOfStockSubscriptionModels.ToList(), Total = totalCount };
        return Json(gridModel);
    }

    #endregion
}
