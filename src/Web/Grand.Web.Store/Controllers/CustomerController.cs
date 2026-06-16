using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.SharedKernel;
using Grand.SharedKernel.Extensions;
using Grand.Web.AdminShared.Extensions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Models;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Grand.Web.Store.Controllers;

/// <summary>
///     Lets a store manager manage the registered customers that belong to his own store.
///     The customer is always forced into the 'Registered' group and tied to the current store; the manager
///     never sees or sets the role, owner, vendor, staff-store or store fields, and the customer-notes /
///     documents tabs are not exposed. Export and impersonation are intentionally not available here.
/// </summary>
[PermissionAuthorize(PermissionSystemName.Customers)]
public class CustomerController : BaseStoreController
{
    #region Constructors

    public CustomerController(
        ICustomerService customerService,
        ICustomerViewModelService customerViewModelService,
        ICustomerManagerService customerManagerService,
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
        CustomerConfig customerConfig)
    {
        _customerService = customerService;
        _customerViewModelService = customerViewModelService;
        _customerManagerService = customerManagerService;
        _productReviewService = productReviewService;
        _productReviewViewModelService = productReviewViewModelService;
        _productViewModelService = productViewModelService;
        _customerAttributeParser = customerAttributeParser;
        _customerAttributeService = customerAttributeService;
        _addressAttributeParser = addressAttributeParser;
        _addressAttributeService = addressAttributeService;
        _messageProviderService = messageProviderService;
        _groupService = groupService;
        _translationService = translationService;
        _contextAccessor = contextAccessor;
        _customerSettings = customerSettings;
        _customerConfig = customerConfig;
    }

    #endregion

    #region Fields

    private readonly ICustomerService _customerService;
    private readonly ICustomerViewModelService _customerViewModelService;
    private readonly ICustomerManagerService _customerManagerService;
    private readonly IProductReviewService _productReviewService;
    private readonly IProductReviewViewModelService _productReviewViewModelService;
    private readonly IProductViewModelService _productViewModelService;
    private readonly ICustomerAttributeParser _customerAttributeParser;
    private readonly ICustomerAttributeService _customerAttributeService;
    private readonly IAddressAttributeParser _addressAttributeParser;
    private readonly IAddressAttributeService _addressAttributeService;
    private readonly IMessageProviderService _messageProviderService;
    private readonly IGroupService _groupService;
    private readonly ITranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;
    private readonly CustomerSettings _customerSettings;
    private readonly CustomerConfig _customerConfig;

    #endregion

    #region Per-store gate

    //managing customers from the store panel only makes sense when customer identity is scoped per store
    //(Customer:RegisterCustomersPerStore). When it's off the whole controller is disabled and every action
    //is routed to the PerStoreDisabled page that explains how to enable the setting.
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!_customerConfig.RegisterCustomersPerStore &&
            context.ActionDescriptor is ControllerActionDescriptor { ActionName: not nameof(PerStoreDisabled) })
        {
            context.Result = RedirectToAction(nameof(PerStoreDisabled));
            return;
        }

        await next();
    }

    #endregion

    #region Utilities

    private string CurrentStoreId => _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    /// <summary>
    ///     Loads a customer only when it is a non-deleted, registered customer of the current store.
    ///     Returns null otherwise so callers can deny access.
    /// </summary>
    private async Task<Customer> GetStoreCustomer(string id)
    {
        var customer = await _customerService.GetCustomerById(id);
        if (customer == null || customer.Deleted || customer.StoreId != CurrentStoreId)
            return null;
        if (!await _groupService.IsRegistered(customer))
            return null;
        return customer;
    }

    /// <summary>
    ///     Forces the store-scoped, registered-only constraints on the posted model regardless of what was sent,
    ///     so the reused AdminShared insert/update path can never assign a foreign store, role or ownership.
    /// </summary>
    private async Task ApplyStoreConstraints(CustomerModel model)
    {
        model.StoreId = CurrentStoreId;
        model.Owner = "";
        model.VendorId = "";
        model.StaffStoreId = "";
        model.SeId = "";
        var registered = await _groupService.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Registered);
        model.CustomerGroups = registered != null ? new[] { registered.Id } : Array.Empty<string>();
    }

    private async Task<IList<CustomAttribute>> ParseCustomCustomerAttributes(IList<CustomAttributeModel> model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var customAttributes = new List<CustomAttribute>();
        var customerAttributes = await _customerAttributeService.GetAllCustomerAttributes();
        foreach (var attribute in customerAttributes)
        {
            switch (attribute.AttributeControlTypeId)
            {
                case AttributeControlType.DropdownList:
                case AttributeControlType.RadioList:
                {
                    var ctrlAttributes = model.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    if (!string.IsNullOrEmpty(ctrlAttributes))
                        customAttributes = _customerAttributeParser
                            .AddCustomerAttribute(customAttributes, attribute, ctrlAttributes).ToList();
                }
                    break;
                case AttributeControlType.Checkboxes:
                {
                    var cblAttributes = model.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    if (!string.IsNullOrEmpty(cblAttributes))
                        foreach (var item in cblAttributes.Split(',').Where(x => !string.IsNullOrEmpty(x)))
                            customAttributes = _customerAttributeParser
                                .AddCustomerAttribute(customAttributes, attribute, item).ToList();
                }
                    break;
                case AttributeControlType.ReadonlyCheckboxes:
                {
                    foreach (var selectedAttributeId in attribute.CustomerAttributeValues
                                 .Where(v => v.IsPreSelected).Select(v => v.Id).ToList())
                        customAttributes = _customerAttributeParser
                            .AddCustomerAttribute(customAttributes, attribute, selectedAttributeId).ToList();
                }
                    break;
                case AttributeControlType.TextBox:
                case AttributeControlType.MultilineTextbox:
                {
                    var ctrlAttributes = model.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    if (!string.IsNullOrEmpty(ctrlAttributes))
                        customAttributes = _customerAttributeParser
                            .AddCustomerAttribute(customAttributes, attribute, ctrlAttributes.Trim()).ToList();
                }
                    break;
                default:
                    break;
            }
        }

        return customAttributes;
    }

    #endregion

    #region Customers

    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> List()
    {
        var model = await _customerViewModelService.PrepareCustomerListModel();
        return View(model);
    }

    /// <summary>
    ///     Shown instead of the panel when per-store customer identity is disabled (see the gate below).
    /// </summary>
    public IActionResult PerStoreDisabled()
    {
        return View();
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> CustomerList(DataSourceRequest command, CustomerListModel model)
    {
        //store managers only ever see the registered customers of their own store
        var registered = await _groupService.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Registered);
        var (customerModelList, totalCount) = await _customerViewModelService.PrepareCustomerList(model,
            registered != null ? [registered.Id] : [], null, command.Page, command.PageSize, CurrentStoreId);
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
        await _customerViewModelService.PrepareCustomerModel(model, null, false);
        await ApplyStoreConstraints(model);
        model.Active = true;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Create(CustomerModel model, bool continueEditing)
    {
        await ApplyStoreConstraints(model);

        if (ModelState.IsValid)
        {
            model.Attributes = await ParseCustomCustomerAttributes(model.SelectedAttributes);
            var customer = await _customerViewModelService.InsertCustomerModel(model);

            //password
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var changePassRequest = new ChangePasswordRequest(model.Email, _customerSettings.DefaultPasswordFormat,
                    model.Password);
                await _customerManagerService.ChangePassword(changePassRequest, customer.StoreId);
            }

            Success(_translationService.GetResource("Admin.Customers.Customers.Added"));
            return continueEditing ? RedirectToAction("Edit", new { id = customer.Id }) : RedirectToAction("List");
        }

        //If we got this far, something failed, redisplay form
        await _customerViewModelService.PrepareCustomerModel(model, null, true);
        await ApplyStoreConstraints(model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var customer = await GetStoreCustomer(id);
        if (customer == null)
            return RedirectToAction("List");

        var model = new CustomerModel();
        await _customerViewModelService.PrepareCustomerModel(model, customer, false);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(CustomerModel model, bool continueEditing)
    {
        var customer = await GetStoreCustomer(model.Id);
        if (customer == null)
            return RedirectToAction("List");

        await ApplyStoreConstraints(model);

        if (ModelState.IsValid)
            try
            {
                model.Attributes = await ParseCustomCustomerAttributes(model.SelectedAttributes);
                customer = await _customerViewModelService.UpdateCustomerModel(customer, model);
                //change password
                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    var changePassRequest = new ChangePasswordRequest(model.Email,
                        _customerSettings.DefaultPasswordFormat, model.Password);
                    await _customerManagerService.ChangePassword(changePassRequest, customer.StoreId);
                }

                Success(_translationService.GetResource("Admin.Customers.Customers.Updated"));
                if (continueEditing)
                {
                    await SaveSelectedTabIndex();
                    return RedirectToAction("Edit", new { id = customer.Id });
                }

                return RedirectToAction("List");
            }
            catch (GrandException exc)
            {
                Error(exc.Message);
            }

        //If we got this far, something failed, redisplay form
        await _customerViewModelService.PrepareCustomerModel(model, customer, true);
        await ApplyStoreConstraints(model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var customer = await GetStoreCustomer(id);
        if (customer == null)
            return RedirectToAction("List");

        if (customer.Id == _contextAccessor.WorkContext.CurrentCustomer.Id)
        {
            Error(_translationService.GetResource("Admin.Customers.Customers.NoSelfDelete"));
            return RedirectToAction("List");
        }

        try
        {
            if (ModelState.IsValid)
            {
                await _customerViewModelService.DeleteCustomer(customer);
                Success(_translationService.GetResource("Admin.Customers.Customers.Deleted"));
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

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> MarkVatNumberAsValid(string id)
    {
        var customer = await GetStoreCustomer(id);
        if (customer == null)
            return RedirectToAction("List");

        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.VatNumberStatusId,
            (int)VatNumberStatus.Valid);

        return RedirectToAction("Edit", new { id = customer.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> MarkVatNumberAsInvalid(string id)
    {
        var customer = await GetStoreCustomer(id);
        if (customer == null)
            return RedirectToAction("List");

        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.VatNumberStatusId,
            (int)VatNumberStatus.Invalid);

        return RedirectToAction("Edit", new { id = customer.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SendWelcomeMessage(string id)
    {
        var customer = await GetStoreCustomer(id);
        if (customer == null)
            return RedirectToAction("List");

        await _messageProviderService.SendCustomerWelcomeMessage(customer, _contextAccessor.StoreContext.CurrentStore,
            _contextAccessor.WorkContext.WorkingLanguage.Id);

        Success(_translationService.GetResource("Admin.Customers.Customers.SendWelcomeMessage.Success"));
        return RedirectToAction("Edit", new { id = customer.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ReSendActivationMessage(string id)
    {
        var customer = await GetStoreCustomer(id);
        if (customer == null)
            return RedirectToAction("List");

        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.AccountActivationToken,
            Guid.NewGuid().ToString());
        await _messageProviderService.SendCustomerEmailValidationMessage(customer,
            _contextAccessor.StoreContext.CurrentStore, _contextAccessor.WorkContext.WorkingLanguage.Id);

        Success(_translationService.GetResource("Admin.Customers.Customers.ReSendActivationMessage.Success"));
        return RedirectToAction("Edit", new { id = customer.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> SendEmail(CustomerModel.SendEmailModel model)
    {
        var customer = await GetStoreCustomer(model.Id);
        if (customer == null)
            return RedirectToAction("List");

        try
        {
            if (string.IsNullOrWhiteSpace(customer.Email))
                throw new GrandException("Customer email is empty");
            if (!CommonHelper.IsValidEmail(customer.Email))
                throw new GrandException("Customer email is not valid");
            if (string.IsNullOrWhiteSpace(model.Subject))
                throw new GrandException("Email subject is empty");
            if (string.IsNullOrWhiteSpace(model.Body))
                throw new GrandException("Email body is empty");

            await _customerViewModelService.SendEmail(customer, model);
            Success(_translationService.GetResource("Admin.Customers.Customers.SendEmail.Queued"));
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
        var customer = await GetStoreCustomer(customerId);
        if (customer == null)
            throw new ArgumentException("No customer found with the specified id");

        var model = (await _customerViewModelService.PrepareLoyaltyPointsHistoryModel(customerId)).ToList();
        var gridModel = new DataSourceResult {
            Data = model,
            Total = model.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> LoyaltyPointsHistoryAdd(string customerId, string storeId,
        int addLoyaltyPointsValue, string addLoyaltyPointsMessage)
    {
        var customer = await GetStoreCustomer(customerId);
        if (customer == null)
            return Json(new { Result = false });

        await _customerViewModelService.InsertLoyaltyPointsHistory(customer, CurrentStoreId, addLoyaltyPointsValue,
            addLoyaltyPointsMessage);

        return Json(new { Result = true });
    }

    #endregion

    #region Addresses

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> AddressesSelect(string customerId, DataSourceRequest command)
    {
        var customer = await GetStoreCustomer(customerId);
        if (customer == null)
            throw new ArgumentException("No customer found with the specified id", nameof(customerId));

        var addresses = (await _customerViewModelService.PrepareAddressModel(customer)).ToList();
        var gridModel = new DataSourceResult {
            Data = addresses,
            Total = addresses.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AddressDelete(string id, string customerId)
    {
        var customer = await GetStoreCustomer(customerId);
        if (customer == null)
            throw new ArgumentException("No customer found with the specified id", nameof(customerId));

        var address = customer.Addresses.FirstOrDefault(a => a.Id == id);
        if (address == null)
            return Content("No customer found with the specified id");
        if (ModelState.IsValid)
        {
            await _customerViewModelService.DeleteAddress(customer, address);
            return new JsonResult("");
        }

        return ErrorForKendoGridJson(ModelState);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> AddressCreate(string customerId)
    {
        var customer = await GetStoreCustomer(customerId);
        if (customer == null)
            return RedirectToAction("List");

        var model = new CustomerAddressModel();
        await _customerViewModelService.PrepareAddressModel(model, null, customer, false);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AddressCreate(CustomerAddressModel model)
    {
        var customer = await GetStoreCustomer(model.CustomerId);
        if (customer == null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            var customAttributes =
                await model.Address.ParseCustomAddressAttributes(_addressAttributeParser, _addressAttributeService);
            var address = await _customerViewModelService.InsertAddressModel(customer, model, customAttributes);
            Success(_translationService.GetResource("Admin.Customers.Customers.Addresses.Added"));
            return RedirectToAction("AddressEdit", new { addressId = address.Id, customerId = model.CustomerId });
        }

        await _customerViewModelService.PrepareAddressModel(model, null, customer, true);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> AddressEdit(string addressId, string customerId)
    {
        var customer = await GetStoreCustomer(customerId);
        if (customer == null)
            return RedirectToAction("List");

        var address = customer.Addresses.FirstOrDefault(x => x.Id == addressId);
        if (address == null)
            return RedirectToAction("Edit", new { id = customer.Id });

        var model = new CustomerAddressModel();
        await _customerViewModelService.PrepareAddressModel(model, address, customer, false);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AddressEdit(CustomerAddressModel model)
    {
        var customer = await GetStoreCustomer(model.CustomerId);
        if (customer == null)
            return RedirectToAction("List");

        var address = customer.Addresses.FirstOrDefault(x => x.Id == model.Address.Id);
        if (address == null)
            return RedirectToAction("Edit", new { id = customer.Id });

        if (ModelState.IsValid)
        {
            var customAttributes =
                await model.Address.ParseCustomAddressAttributes(_addressAttributeParser, _addressAttributeService);
            address = await _customerViewModelService.UpdateAddressModel(customer, address, model, customAttributes);
            Success(_translationService.GetResource("Admin.Customers.Customers.Addresses.Updated"));
            return RedirectToAction("AddressEdit", new { addressId = model.Address.Id, customerId = model.CustomerId });
        }

        await _customerViewModelService.PrepareAddressModel(model, address, customer, true);
        return View(model);
    }

    #endregion

    #region Orders

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> OrderList(string customerId, DataSourceRequest command,
        [FromServices] IOrderViewModelService orderViewModelService)
    {
        var customer = await GetStoreCustomer(customerId);
        if (customer == null)
            return Json(new DataSourceResult { Data = null, Total = 0 });

        var model = new OrderListModel {
            CustomerId = customerId,
            StoreId = CurrentStoreId
        };

        var (orderModels, totalCount) =
            await orderViewModelService.PrepareOrderModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = orderModels.ToList(),
            Total = totalCount
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> OrderDetails(string orderId,
        [FromServices] IOrderService orderService, [FromServices] IOrderViewModelService orderViewModelService)
    {
        var order = await orderService.GetOrderById(orderId);
        if (order == null || order.StoreId != CurrentStoreId)
            return Json(new DataSourceResult { Data = null, Total = 0 });

        var ordermodel = new OrderModel();
        await orderViewModelService.PrepareOrderDetailsModel(ordermodel, order);
        var gridModel = new DataSourceResult {
            Data = ordermodel.Items,
            Total = ordermodel.Items.Count
        };

        return Json(gridModel);
    }

    #endregion

    #region Reviews

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ReviewList(string customerId, DataSourceRequest command)
    {
        var customer = await GetStoreCustomer(customerId);
        if (customer == null)
            return Json(new DataSourceResult { Data = null, Total = 0 });

        var productReviews = await _productReviewService.GetAllProductReviews(customerId, null,
            null, null, "", CurrentStoreId, "", command.Page - 1, command.PageSize);
        var items = new List<ProductReviewModel>();
        foreach (var x in productReviews)
        {
            var m = new ProductReviewModel();
            await _productViewModelService.PrepareProductReviewModel(m, x, false, true);
            items.Add(m);
        }

        var gridModel = new DataSourceResult {
            Data = items,
            Total = productReviews.TotalCount
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ReviewDelete(string id)
    {
        var productReview = await _productReviewService.GetProductReviewById(id);
        if (productReview == null || productReview.StoreId != CurrentStoreId)
            throw new ArgumentException("No review found with the specified id", nameof(id));

        await _productReviewViewModelService.DeleteProductReview(productReview);
        return new JsonResult("");
    }

    #endregion

    #region Current shopping cart / wishlist

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> GetCartList(string customerId, int cartTypeId)
    {
        var customer = await GetStoreCustomer(customerId);
        if (customer == null)
            return Json(new DataSourceResult { Data = null, Total = 0 });

        var cart = await _customerViewModelService.PrepareShoppingCartItemModel(customerId, cartTypeId);
        var gridModel = new DataSourceResult {
            Data = cart,
            Total = cart.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> UpdateCart(string id, string customerId, double? unitPriceValue)
    {
        var customer = await GetStoreCustomer(customerId);
        if (customer == null)
            throw new ArgumentException("No customer found with the specified id", nameof(customerId));

        var warnings = await _customerViewModelService.UpdateCart(customer, id, unitPriceValue);
        if (warnings.Any())
            return ErrorForKendoGridJson(string.Join(",", warnings));

        return new JsonResult("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> DeleteCart(string id, string customerId)
    {
        var customer = await GetStoreCustomer(customerId);
        if (customer == null)
            throw new ArgumentException("No customer found with the specified id", nameof(customerId));

        await _customerViewModelService.DeleteCart(customer, id);
        return new JsonResult("");
    }

    #endregion

    #region Customer Product Personalize / Price

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ProductsPrice(DataSourceRequest command, string customerId)
    {
        var customer = await GetStoreCustomer(customerId);
        if (customer == null)
            return Json(new DataSourceResult { Data = null, Total = 0 });

        var (productPriceModels, totalCount) =
            await _customerViewModelService.PrepareProductPriceModel(customerId, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = productPriceModels.ToList(),
            Total = totalCount
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> PersonalizedProducts(DataSourceRequest command, string customerId)
    {
        var customer = await GetStoreCustomer(customerId);
        if (customer == null)
            return Json(new DataSourceResult { Data = null, Total = 0 });

        var (productModels, totalCount) =
            await _customerViewModelService.PreparePersonalizedProducts(customerId, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = productModels.ToList(),
            Total = totalCount
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ProductAddPopup(string customerId)
    {
        var customer = await GetStoreCustomer(customerId);
        if (customer == null)
            return RedirectToAction("List");

        var model = await _customerViewModelService.PrepareCustomerModelAddProductModel();
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAddPopupList(DataSourceRequest command, CustomerModel.AddProductModel model)
    {
        var products = await _customerViewModelService.PrepareProductModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = products.products.ToList(),
            Total = products.totalCount
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ProductAddPopup(string customerId, bool personalized,
        CustomerModel.AddProductModel model)
    {
        var customer = await GetStoreCustomer(customerId);
        if (customer == null)
            return Content("");

        if (model.SelectedProductIds != null)
            await _customerViewModelService.InsertCustomerAddProductModel(customerId, personalized, model);
        return Content("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> UpdateProductPrice(CustomerModel.ProductPriceModel model)
    {
        await _customerViewModelService.UpdateProductPrice(model);
        return new JsonResult("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> DeleteProductPrice(string id)
    {
        await _customerViewModelService.DeleteProductPrice(id);
        return new JsonResult("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> UpdatePersonalizedProduct(CustomerModel.ProductModel model)
    {
        await _customerViewModelService.UpdatePersonalizedProduct(model);
        return new JsonResult("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> DeletePersonalizedProduct(string id)
    {
        await _customerViewModelService.DeletePersonalizedProduct(id);
        return new JsonResult("");
    }

    #endregion

    #region Out of stock subscriptions

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> OutOfStockSubscriptionList(DataSourceRequest command, string customerId)
    {
        var customer = await GetStoreCustomer(customerId);
        if (customer == null)
            return Json(new DataSourceResult { Data = null, Total = 0 });

        var (outOfStockSubscriptionModels, totalCount) =
            await _customerViewModelService.PrepareOutOfStockSubscriptionModel(customerId, command.Page,
                command.PageSize);
        var gridModel = new DataSourceResult {
            Data = outOfStockSubscriptionModels.ToList(),
            Total = totalCount
        };
        return Json(gridModel);
    }

    #endregion
}
