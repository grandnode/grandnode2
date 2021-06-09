using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Common.Interfaces.Addresses;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Customers.Utilities;
using Grand.Business.Messages.Interfaces;
using Grand.Business.Storage.Interfaces;
using Grand.Business.System.Interfaces.ExportImport;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.SharedKernel;
using Grand.SharedKernel.Extensions;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Catalog;
using Grand.Web.Admin.Models.Customers;
using Grand.Web.Admin.Models.Orders;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Customers)]
    public partial class CustomerController : BaseAdminController
    {
        #region Fields

        private readonly ICustomerService _customerService;
        private readonly IGroupService _groupService;
        private readonly IProductService _productService;
        private readonly IProductReviewService _productReviewService;
        private readonly IProductReviewViewModelService _productReviewViewModelService;
        private readonly IProductViewModelService _productViewModelService;
        private readonly ICustomerViewModelService _customerViewModelService;
        private readonly IUserFieldService _userFieldService;
        private readonly ICustomerManagerService _customerManagerService;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;
        private readonly IExportManager _exportManager;
        private readonly ICustomerAttributeParser _customerAttributeParser;
        private readonly ICustomerAttributeService _customerAttributeService;
        private readonly IAddressAttributeParser _addressAttributeParser;
        private readonly IAddressAttributeService _addressAttributeService;
        private readonly IMessageProviderService _messageProviderService;
        private readonly IDownloadService _downloadService;
        private readonly IPermissionService _permissionService;
        private readonly CustomerSettings _customerSettings;
        #endregion

        #region Constructors

        public CustomerController(ICustomerService customerService,
            IProductService productService,
            IProductReviewService productReviewService,
            IProductReviewViewModelService productReviewViewModelService,
            IProductViewModelService productViewModelService,
            ICustomerViewModelService customerViewModelService,
            IUserFieldService userFieldService,
            ICustomerManagerService customerManagerService,
            ITranslationService translationService,
            IWorkContext workContext,
            IGroupService groupService,
            IExportManager exportManager,
            ICustomerAttributeParser customerAttributeParser,
            ICustomerAttributeService customerAttributeService,
            IAddressAttributeParser addressAttributeParser,
            IAddressAttributeService addressAttributeService,
            IMessageProviderService messageProviderService,
            IDownloadService downloadService,
            IPermissionService permissionService,
            CustomerSettings customerSettings)
        {
            _customerService = customerService;
            _productService = productService;
            _productReviewService = productReviewService;
            _productReviewViewModelService = productReviewViewModelService;
            _productViewModelService = productViewModelService;
            _customerViewModelService = customerViewModelService;
            _userFieldService = userFieldService;
            _customerManagerService = customerManagerService;
            _translationService = translationService;
            _workContext = workContext;
            _groupService = groupService;
            _exportManager = exportManager;
            _customerAttributeParser = customerAttributeParser;
            _customerAttributeService = customerAttributeService;
            _addressAttributeParser = addressAttributeParser;
            _addressAttributeService = addressAttributeService;
            _messageProviderService = messageProviderService;
            _downloadService = downloadService;
            _permissionService = permissionService;
            _customerSettings = customerSettings;
        }

        #endregion

        protected virtual async Task<IList<CustomAttribute>> ParseCustomCustomerAttributes(IFormCollection form)
        {
            if (form == null)
                throw new ArgumentNullException(nameof(form));

            var customAttributes = new List<CustomAttribute>();
            var customerAttributes = await _customerAttributeService.GetAllCustomerAttributes();
            foreach (var attribute in customerAttributes)
            {
                string controlId = string.Format("customer_attribute_{0}", attribute.Id);
                switch (attribute.AttributeControlTypeId)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                        {
                            form.TryGetValue(controlId, out var ctrlAttributes);
                            if (!string.IsNullOrEmpty(ctrlAttributes))
                            {
                                customAttributes = _customerAttributeParser.AddCustomerAttribute(customAttributes,
                                    attribute, ctrlAttributes).ToList();
                            }
                        }
                        break;
                    case AttributeControlType.Checkboxes:
                        {
                            form.TryGetValue(controlId, out var cblAttributes);
                            if (!string.IsNullOrEmpty(cblAttributes))
                            {
                                foreach (var item in cblAttributes)
                                {
                                    if (!string.IsNullOrEmpty(item))
                                        customAttributes = _customerAttributeParser.AddCustomerAttribute(customAttributes,
                                            attribute, item).ToList();
                                }
                            }
                        }
                        break;
                    case AttributeControlType.ReadonlyCheckboxes:
                        {
                            //load read-only (already server-side selected) values
                            var attributeValues = attribute.CustomerAttributeValues;
                            foreach (var selectedAttributeId in attributeValues
                                .Where(v => v.IsPreSelected)
                                .Select(v => v.Id)
                                .ToList())
                            {
                                customAttributes = _customerAttributeParser.AddCustomerAttribute(customAttributes,
                                            attribute, selectedAttributeId).ToList();
                            }
                        }
                        break;
                    case AttributeControlType.TextBox:
                    case AttributeControlType.MultilineTextbox:
                        {
                            form.TryGetValue(controlId, out var ctrlAttributes);
                            if (!string.IsNullOrEmpty(ctrlAttributes))
                            {
                                string enteredText = ctrlAttributes.ToString().Trim();
                                customAttributes = _customerAttributeParser.AddCustomerAttribute(customAttributes,
                                    attribute, enteredText).ToList();
                            }
                        }
                        break;
                    case AttributeControlType.Datepicker:
                    case AttributeControlType.ColorSquares:
                    case AttributeControlType.ImageSquares:
                    case AttributeControlType.FileUpload:
                    //not supported customer attributes
                    default:
                        break;
                }
            }

            return customAttributes;
        }

        protected virtual async Task<bool> CheckSalesManager(Customer customer)
        {
            return (await _groupService.IsSalesManager(_workContext.CurrentCustomer)
                && (_workContext.CurrentCustomer.SeId != customer.SeId));
        }

        #region Customers

        public IActionResult Index() => RedirectToAction("List");

        public async Task<IActionResult> List()
        {
            var model = await _customerViewModelService.PrepareCustomerListModel();
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> CustomerList(DataSourceRequest command, CustomerListModel model,
            string[] searchCustomerGroupIds, string[] searchCustomerTagIds)
        {
            var (customerModelList, totalCount) = await _customerViewModelService.PrepareCustomerList(model, searchCustomerGroupIds, searchCustomerTagIds, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
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
            //default value
            model.Active = true;
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Create(CustomerModel model, bool continueEditing, IFormCollection form)
        {
            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                var cust2 = await _customerService.GetCustomerByEmail(model.Email);
                if (cust2 != null)
                    ModelState.AddModelError("", "Email is already registered");
            }

            if (!string.IsNullOrWhiteSpace(model.Owner))
            {
                var custowner = await _customerService.GetCustomerByEmail(model.Owner);
                if (custowner == null)
                    ModelState.AddModelError("", "Owner email is not exists");
            }

            if (!string.IsNullOrWhiteSpace(model.Username) & _customerSettings.UsernamesEnabled)
            {
                var cust2 = await _customerService.GetCustomerByUsername(model.Username);
                if (cust2 != null)
                    ModelState.AddModelError("", "Username is already registered");
            }

            //validate customer groups
            var allCustomerGroups = await _groupService.GetAllCustomerGroups(showHidden: true);
            var newCustomerGroups = new List<CustomerGroup>();
            foreach (var customerGroup in allCustomerGroups)
                if (model.CustomerGroups != null && model.CustomerGroups.Contains(customerGroup.Id))
                    newCustomerGroups.Add(customerGroup);
            var customerGroupsError = await _customerViewModelService.ValidateCustomerGroups(newCustomerGroups);
            if (!string.IsNullOrEmpty(customerGroupsError))
            {
                ModelState.AddModelError("", customerGroupsError);
                Error(customerGroupsError, false);
            }

            if (ModelState.IsValid)
            {
                model.Attributes = await ParseCustomCustomerAttributes(form);
                var customer = await _customerViewModelService.InsertCustomerModel(model);

                //password
                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    var changePassRequest = new ChangePasswordRequest(model.Email, false, _customerSettings.DefaultPasswordFormat, model.Password);
                    var changePassResult = await _customerManagerService.ChangePassword(changePassRequest);
                    if (!changePassResult.Success)
                    {
                        foreach (var changePassError in changePassResult.Errors)
                            Error(changePassError);
                    }
                }
                if (await _groupService.IsAdmin(customer) && !string.IsNullOrEmpty(model.VendorId))
                {
                    Error(_translationService.GetResource("Admin.Customers.Customers.AdminCouldNotbeVendor"));
                }
                if (newCustomerGroups.Any(x => x.SystemName == SystemCustomerGroupNames.Vendors) && string.IsNullOrEmpty(model.VendorId))
                {
                    Error(_translationService.GetResource("Admin.Customers.Customers.CannotBeInVendoGroupWithoutVendorAssociated"));
                }
                if (newCustomerGroups.Any(x => x.SystemName == SystemCustomerGroupNames.Staff) && string.IsNullOrEmpty(model.StaffStoreId))
                {
                    Error(_translationService.GetResource("Admin.Customers.Customers.CannotBeInStaffGroupWithoutStaffAssociated"));
                }
                if (newCustomerGroups.Any(x => x.SystemName == SystemCustomerGroupNames.SalesManager) && string.IsNullOrEmpty(model.SeId))
                {
                    Error(_translationService.GetResource("Admin.Customers.Customers.CannotBeInSalesManagerGroupWithoutSalesEmployeeAssociated"));
                }
                if (await _groupService.IsStaff(customer) && await _groupService.IsVendor(customer))
                {
                    Error(_translationService.GetResource("Admin.Customers.Customers.VendorShouldNotbeStaff"));
                }

                Success(_translationService.GetResource("Admin.Customers.Customers.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = customer.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            await _customerViewModelService.PrepareCustomerModel(model, null, true);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var customer = await _customerService.GetCustomerById(id);
            if (customer == null || customer.Deleted || await CheckSalesManager(customer))
                //No customer found with the specified id
                return RedirectToAction("List");

            var model = new CustomerModel();
            await _customerViewModelService.PrepareCustomerModel(model, customer, false);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> Edit(CustomerModel model, bool continueEditing, IFormCollection form)
        {
            var customer = await _customerService.GetCustomerById(model.Id);
            if (customer == null || customer.Deleted || await CheckSalesManager(customer))
                //No customer found with the specified id
                return RedirectToAction("List");

            //validate customer groups
            var allCustomerGroups = await _groupService.GetAllCustomerGroups(showHidden: true);
            var newCustomerGroups = new List<CustomerGroup>();
            foreach (var customerGroup in allCustomerGroups)
                if (model.CustomerGroups != null && model.CustomerGroups.Contains(customerGroup.Id))
                    newCustomerGroups.Add(customerGroup);

            var customerGroupsError = await _customerViewModelService.ValidateCustomerGroups(newCustomerGroups);
            if (!string.IsNullOrEmpty(customerGroupsError))
            {
                ModelState.AddModelError("", customerGroupsError);
                Error(customerGroupsError, false);
            }
            if (!string.IsNullOrWhiteSpace(model.Owner))
            {
                var custowner = await _customerService.GetCustomerByEmail(model.Owner);
                if (custowner == null)
                    ModelState.AddModelError("", "Owner email is not exists");

                if (model.Owner.ToLower() == model.Email.ToLower())
                    ModelState.AddModelError("", "You can't assign own email");
            }

            if (await _groupService.IsSalesManager(_workContext.CurrentCustomer) && customer.Id == _workContext.CurrentCustomer.Id)
                ModelState.AddModelError("", "You can't edit own data from admin panel");

            if (ModelState.IsValid)
            {
                try
                {
                    model.Attributes = await ParseCustomCustomerAttributes(form);
                    customer = await _customerViewModelService.UpdateCustomerModel(customer, model);
                    //change password
                    if (!string.IsNullOrWhiteSpace(model.Password))
                    {
                        var changePassRequest = new ChangePasswordRequest(model.Email, false, _customerSettings.DefaultPasswordFormat, model.Password);
                        var changePassResult = await _customerManagerService.ChangePassword(changePassRequest);
                        if (!changePassResult.Success)
                        {
                            foreach (var changePassError in changePassResult.Errors)
                                Error(changePassError);
                        }
                    }
                    if (await _groupService.IsAdmin(customer) && !string.IsNullOrEmpty(model.VendorId))
                    {
                        Error(_translationService.GetResource("Admin.Customers.Customers.AdminCouldNotbeVendor"));
                    }
                    if (newCustomerGroups.Any(x => x.SystemName == SystemCustomerGroupNames.Vendors) && string.IsNullOrEmpty(model.VendorId))
                    {
                        Error(_translationService.GetResource("Admin.Customers.Customers.CannotBeInVendoGroupWithoutVendorAssociated"));
                    }
                    if (newCustomerGroups.Any(x => x.SystemName == SystemCustomerGroupNames.Staff) && string.IsNullOrEmpty(model.StaffStoreId))
                    {
                        Error(_translationService.GetResource("Admin.Customers.Customers.CannotBeInStaffGroupWithoutStaffAssociated"));
                    }
                    if (newCustomerGroups.Any(x => x.SystemName == SystemCustomerGroupNames.SalesManager) && string.IsNullOrEmpty(model.SeId))
                    {
                        Error(_translationService.GetResource("Admin.Customers.Customers.CannotBeInSalesManagerGroupWithoutSalesEmployeeAssociated"));
                    }
                    if (await _groupService.IsStaff(customer) && await _groupService.IsVendor(customer))
                    {
                        Warning(_translationService.GetResource("Admin.Customers.Customers.VendorShouldNotbeStaff"));
                    }
                    Success(_translationService.GetResource("Admin.Customers.Customers.Updated"));
                    if (continueEditing)
                    {
                        //selected tab
                        await SaveSelectedTabIndex();

                        return RedirectToAction("Edit", new { id = customer.Id });
                    }
                    return RedirectToAction("List");
                }
                catch (Exception exc)
                {
                    Error(exc.Message, false);
                }
            }
            //If we got this far, something failed, redisplay form
            await _customerViewModelService.PrepareCustomerModel(model, customer, true);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> MarkVatNumberAsValid(CustomerModel model)
        {
            var customer = await _customerService.GetCustomerById(model.Id);
            if (customer == null || customer.Deleted || await CheckSalesManager(customer))
                //No customer found with the specified id
                return RedirectToAction("List");

            await _userFieldService.SaveField(customer,
                SystemCustomerFieldNames.VatNumberStatusId,
                (int)VatNumberStatus.Valid);

            return RedirectToAction("Edit", new { id = customer.Id });
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> MarkVatNumberAsInvalid(CustomerModel model)
        {
            var customer = await _customerService.GetCustomerById(model.Id);
            if (customer == null || customer.Deleted || await CheckSalesManager(customer))
                //No customer found with the specified id
                return RedirectToAction("List");

            await _userFieldService.SaveField(customer,
                SystemCustomerFieldNames.VatNumberStatusId,
                (int)VatNumberStatus.Invalid);

            return RedirectToAction("Edit", new { id = customer.Id });
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> RemoveAffiliate(CustomerModel model)
        {
            var customer = await _customerService.GetCustomerById(model.Id);
            if (customer == null || customer.Deleted || await CheckSalesManager(customer))
                //No customer found with the specified id
                return RedirectToAction("List");

            customer.AffiliateId = "";
            await _customerService.UpdateCustomerField(customer.Id, x => x.AffiliateId, customer.AffiliateId);
            return RedirectToAction("Edit", new { id = customer.Id });
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var customer = await _customerService.GetCustomerById(id);
            if (customer == null || customer.Deleted || await CheckSalesManager(customer))
                //No customer found with the specified id
                return RedirectToAction("List");

            if (customer.Id == _workContext.CurrentCustomer.Id)
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

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        public async Task<IActionResult> DeleteSelected(ICollection<string> selectedIds)
        {
            if (selectedIds != null)
            {
                await _customerViewModelService.DeleteSelected(selectedIds.ToList());
            }

            return Json(new { Result = true });
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> Impersonate(string id)
        {
            var customer = await _customerService.GetCustomerById(id);
            if (customer == null || customer.Deleted || await CheckSalesManager(customer))
                //No customer found with the specified id
                return RedirectToAction("List");

            //ensure that a non-admin user cannot impersonate as an administrator
            //otherwise, that user can simply impersonate as an administrator and gain additional administrative privileges
            if (!await _groupService.IsAdmin(_workContext.CurrentCustomer) && await _groupService.IsAdmin(customer))
            {
                Error("A non-admin user cannot impersonate as an administrator");
                return RedirectToAction("Edit", customer.Id);
            }

            await _userFieldService.SaveField<string>(_workContext.CurrentCustomer,
                SystemCustomerFieldNames.ImpersonatedCustomerId, customer.Id);

            return RedirectToAction("Index", "Home", new { area = "" });
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> SendWelcomeMessage(CustomerModel model)
        {
            var customer = await _customerService.GetCustomerById(model.Id);
            if (customer == null || customer.Deleted || await CheckSalesManager(customer))
                //No customer found with the specified id
                return RedirectToAction("List");

            await _messageProviderService.SendCustomerWelcomeMessage(customer, _workContext.CurrentStore, _workContext.WorkingLanguage.Id);

            Success(_translationService.GetResource("Admin.Customers.Customers.SendWelcomeMessage.Success"));

            return RedirectToAction("Edit", new { id = customer.Id });
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> ReSendActivationMessage(CustomerModel model)
        {
            var customer = await _customerService.GetCustomerById(model.Id);
            if (customer == null || customer.Deleted || await CheckSalesManager(customer))
                //No customer found with the specified id
                return RedirectToAction("List");

            //email validation message
            await _userFieldService.SaveField(customer, SystemCustomerFieldNames.AccountActivationToken, Guid.NewGuid().ToString());
            await _messageProviderService.SendCustomerEmailValidationMessage(customer, _workContext.CurrentStore, _workContext.WorkingLanguage.Id);

            Success(_translationService.GetResource("Admin.Customers.Customers.ReSendActivationMessage.Success"));

            return RedirectToAction("Edit", new { id = customer.Id });
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> SendEmail(CustomerModel model)
        {
            var customer = await _customerService.GetCustomerById(model.Id);
            if (customer == null || customer.Deleted || await CheckSalesManager(customer))
                //No customer found with the specified id
                return RedirectToAction("List");

            try
            {
                if (String.IsNullOrWhiteSpace(customer.Email))
                    throw new GrandException("Customer email is empty");
                if (!CommonHelper.IsValidEmail(customer.Email))
                    throw new GrandException("Customer email is not valid");
                if (String.IsNullOrWhiteSpace(model.SendEmail.Subject))
                    throw new GrandException("Email subject is empty");
                if (String.IsNullOrWhiteSpace(model.SendEmail.Body))
                    throw new GrandException("Email body is empty");

                await _customerViewModelService.SendEmail(customer, model.SendEmail);

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
            var customer = await _customerService.GetCustomerById(customerId);
            if (customer == null || customer.Deleted || await CheckSalesManager(customer))
                throw new ArgumentException("No customer found with the specified id");

            var model = (await _customerViewModelService.PrepareLoyaltyPointsHistoryModel(customerId)).ToList();
            var gridModel = new DataSourceResult
            {
                Data = model,
                Total = model.Count
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> LoyaltyPointsHistoryAdd(string customerId, string storeId, int addLoyaltyPointsValue, string addLoyaltyPointsMessage)
        {
            var customer = await _customerService.GetCustomerById(customerId);
            if (customer == null || customer.Deleted || await CheckSalesManager(customer))
                return Json(new { Result = false });

            await _customerViewModelService.InsertLoyaltyPointsHistory(customer, storeId, addLoyaltyPointsValue, addLoyaltyPointsMessage);

            return Json(new { Result = true });
        }

        #endregion

        #region Addresses

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> AddressesSelect(string customerId, DataSourceRequest command)
        {
            var customer = await _customerService.GetCustomerById(customerId);
            if (customer == null || customer.Deleted || await CheckSalesManager(customer))
                throw new ArgumentException("No customer found with the specified id", "customerId");

            var addresses = (await _customerViewModelService.PrepareAddressModel(customer)).ToList();
            var gridModel = new DataSourceResult
            {
                Data = addresses,
                Total = addresses.Count
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> AddressDelete(string id, string customerId)
        {
            var customer = await _customerService.GetCustomerById(customerId);
            if (customer == null || customer.Deleted || await CheckSalesManager(customer))
                throw new ArgumentException("No customer found with the specified id", "customerId");

            var address = customer.Addresses.FirstOrDefault(a => a.Id == id);
            if (address == null)
                //No customer found with the specified id
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
            var customer = await _customerService.GetCustomerById(customerId);
            if (customer == null || customer.Deleted || await CheckSalesManager(customer))
                //No customer found with the specified id
                return RedirectToAction("List");

            var model = new CustomerAddressModel();
            await _customerViewModelService.PrepareAddressModel(model, null, customer, false);

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> AddressCreate(CustomerAddressModel model, IFormCollection form)
        {
            var customer = await _customerService.GetCustomerById(model.CustomerId);
            if (customer == null || customer.Deleted || await CheckSalesManager(customer))
                //No customer found with the specified id
                return RedirectToAction("List");

            //custom address attributes
            var customAttributes = await form.ParseCustomAddressAttributes(_addressAttributeParser, _addressAttributeService);
            var customAttributeWarnings = await _addressAttributeParser.GetAttributeWarnings(customAttributes);
            foreach (var error in customAttributeWarnings)
            {
                ModelState.AddModelError("", error);
            }

            if (ModelState.IsValid)
            {
                var address = await _customerViewModelService.InsertAddressModel(customer, model, customAttributes);
                Success(_translationService.GetResource("Admin.Customers.Customers.Addresses.Added"));
                return RedirectToAction("AddressEdit", new { addressId = address.Id, customerId = model.CustomerId });
            }

            //If we got this far, something failed, redisplay form
            await _customerViewModelService.PrepareAddressModel(model, null, customer, true);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> AddressEdit(string addressId, string customerId)
        {
            var customer = await _customerService.GetCustomerById(customerId);
            if (customer == null || customer.Deleted || await CheckSalesManager(customer))
                //No customer found with the specified id
                return RedirectToAction("List");

            var address = customer.Addresses.Where(x => x.Id == addressId).FirstOrDefault();
            if (address == null)
                //No address found with the specified id
                return RedirectToAction("Edit", new { id = customer.Id });

            var model = new CustomerAddressModel();
            await _customerViewModelService.PrepareAddressModel(model, address, customer, false);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]

        public async Task<IActionResult> AddressEdit(CustomerAddressModel model, IFormCollection form)
        {
            var customer = await _customerService.GetCustomerById(model.CustomerId);
            if (customer == null || customer.Deleted || await CheckSalesManager(customer))
                //No customer found with the specified id
                return RedirectToAction("List");

            var address = customer.Addresses.Where(x => x.Id == model.Address.Id).FirstOrDefault();
            if (address == null)
                //No address found with the specified id
                return RedirectToAction("Edit", new { id = customer.Id });

            //custom address attributes
            var customAttributes = await form.ParseCustomAddressAttributes(_addressAttributeParser, _addressAttributeService);
            var customAttributeWarnings = await _addressAttributeParser.GetAttributeWarnings(customAttributes);
            foreach (var error in customAttributeWarnings)
            {
                ModelState.AddModelError("", error);
            }

            if (ModelState.IsValid)
            {
                address = await _customerViewModelService.UpdateAddressModel(customer, address, model, customAttributes);
                Success(_translationService.GetResource("Admin.Customers.Customers.Addresses.Updated"));
                return RedirectToAction("AddressEdit", new { addressId = model.Address.Id, customerId = model.CustomerId });
            }
            //If we got this far, something failed, redisplay form
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
            if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
                return Json(new DataSourceResult
                {
                    Data = null,
                    Total = 0
                });

            var model = new OrderListModel
            {
                CustomerId = customerId
            };
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                model.StoreId = _workContext.CurrentCustomer.StaffStoreId;
            if (_workContext.CurrentVendor != null)
                model.VendorId = _workContext.CurrentVendor.Id;

            var (orderModels, totalCount) = await orderViewModelService.PrepareOrderModel(model, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
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
            if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
                return Json(new DataSourceResult
                {
                    Data = null,
                    Total = 0
                });

            var order = await orderService.GetOrderById(orderId);
            if (order == null)
                throw new ArgumentException("No order found with the specified id");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !_workContext.HasAccessToOrder(order) && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return Json(new DataSourceResult
                {
                    Data = null,
                    Total = 0
                });

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
                return Json(new DataSourceResult
                {
                    Data = null,
                    Total = 0
                });

            var ordermodel = new OrderModel();
            await orderViewModelService.PrepareOrderDetailsModel(ordermodel, order);
            var gridModel = new DataSourceResult
            {
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
            var productReviews = await _productReviewService.GetAllProductReviews(customerId, null,
                null, null, "", null, "", command.Page - 1, command.PageSize);
            var items = new List<ProductReviewModel>();
            foreach (var x in productReviews)
            {
                var m = new ProductReviewModel();
                await _productViewModelService.PrepareProductReviewModel(m, x, false, true);
                items.Add(m);
            }
            var gridModel = new DataSourceResult
            {
                Data = items,
                Total = productReviews.TotalCount,
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> ReviewDelete(string id)
        {
            var productReview = await _productReviewService.GetProductReviewById(id);
            if (productReview == null)
                throw new ArgumentException("No review found with the specified id", "id");

            await _productReviewViewModelService.DeleteProductReview(productReview);
            return new JsonResult("");
        }

        #endregion

        #region Current shopping cart/ wishlist

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> GetCartList(string customerId, int cartTypeId)
        {
            var cart = await _customerViewModelService.PrepareShoppingCartItemModel(customerId, cartTypeId);
            var gridModel = new DataSourceResult
            {
                Data = cart,
                Total = cart.Count
            };

            return Json(gridModel);
        }
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> UpdateCart(string id, string customerId, double? unitPriceValue)
        {
            var customer = await _customerService.GetCustomerById(customerId);
            if (customer == null || customer.Deleted || await CheckSalesManager(customer))
                throw new ArgumentException("No customer found with the specified id", "customerId");

            var warnings = await _customerViewModelService.UpdateCart(customer, id, unitPriceValue);
            if (warnings.Any())
                return ErrorForKendoGridJson(string.Join(",", warnings));

            return new JsonResult("");
        }
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> DeleteCart(string id, string customerId)
        {
            var customer = await _customerService.GetCustomerById(customerId);
            if (customer == null || customer.Deleted || await CheckSalesManager(customer))
                throw new ArgumentException("No customer found with the specified id", "customerId");

            await _customerViewModelService.DeleteCart(customer, id);

            return new JsonResult("");
        }
        #endregion

        #region Customer Product Personalize / Price

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> ProductsPrice(DataSourceRequest command, string customerId)
        {
            var (productPriceModels, totalCount) = await _customerViewModelService.PrepareProductPriceModel(customerId, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = productPriceModels.ToList(),
                Total = totalCount
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> PersonalizedProducts(DataSourceRequest command, string customerId)
        {
            var (productModels, totalCount) = await _customerViewModelService.PreparePersonalizedProducts(customerId, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = productModels.ToList(),
                Total = totalCount
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ProductAddPopup(string customerId)
        {
            var model = await _customerViewModelService.PrepareCustomerModelAddProductModel();
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> ProductAddPopupList(DataSourceRequest command, CustomerModel.AddProductModel model)
        {
            var products = await _customerViewModelService.PrepareProductModel(model, command.Page, command.PageSize);

            var gridModel = new DataSourceResult
            {
                Data = products.products.ToList(),
                Total = products.totalCount
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> ProductAddPopup(string customerId, bool personalized, CustomerModel.AddProductModel model)
        {
            if (model.SelectedProductIds != null)
            {
                await _customerViewModelService.InsertCustomerAddProductModel(customerId, personalized, model);
            }

            ViewBag.RefreshPage = true;
            return View(model);
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

        #region Activity log and message contact form

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> ListActivityLog(DataSourceRequest command, string customerId)
        {
            var (activityLogModels, totalCount) = await _customerViewModelService.PrepareActivityLogModel(customerId, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = activityLogModels.ToList(),
                Total = totalCount
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> ContactFormList(DataSourceRequest command, string customerId)
        {
            string vendorId = "";
            if (_workContext.CurrentVendor != null)
            {
                vendorId = _workContext.CurrentVendor.Id;
            }
            var (contactFormModels, totalCount) = await _customerViewModelService.PrepareContactFormModel(customerId, vendorId, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = contactFormModels.ToList(),
                Total = totalCount
            };
            return Json(gridModel);
        }
        #endregion

        #region Out of stock subscriptions

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> OutOfStockSubscriptionList(DataSourceRequest command, string customerId)
        {
            var (outOfStockSubscriptionModels, totalCount) = await _customerViewModelService.PrepareOutOfStockSubscriptionModel(customerId, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = outOfStockSubscriptionModels.ToList(),
                Total = totalCount
            };
            return Json(gridModel);
        }

        #endregion

        #region Customer note

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> CustomerNotesSelect(string customerId, DataSourceRequest command)
        {
            var customer = await _customerService.GetCustomerById(customerId);
            if (customer == null || customer.Deleted || await CheckSalesManager(customer))
                throw new ArgumentException("No customer found with the specified id");

            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null)
                return Content("");

            var customerNoteModels = await _customerViewModelService.PrepareCustomerNoteList(customerId);
            var gridModel = new DataSourceResult
            {
                Data = customerNoteModels,
                Total = customerNoteModels.Count
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> CustomerNoteAdd(string customerId, string downloadId, bool displayToCustomer, string title, string message)
        {
            var customer = await _customerService.GetCustomerById(customerId);
            if (customer == null || customer.Deleted || await CheckSalesManager(customer))
                return Json(new { Result = false });

            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null)
                return Json(new { Result = false });

            await _customerViewModelService.InsertCustomerNote(customerId, downloadId, displayToCustomer, title, message);

            return Json(new { Result = true });
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> CustomerNoteDelete(string id, string customerId)
        {
            var customer = await _customerService.GetCustomerById(customerId);
            if (customer == null || customer.Deleted || await CheckSalesManager(customer))
                throw new ArgumentException("No customer found with the specified id");

            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null)
                throw new ArgumentException("AccessDenied");

            await _customerViewModelService.DeleteCustomerNote(id, customerId);

            return new JsonResult("");
        }


        #endregion

        #region Export 

        [PermissionAuthorizeAction(PermissionActionName.Export)]
        [HttpPost]
        public async Task<IActionResult> ExportExcelAll(CustomerListModel model)
        {
            var salesEmployeeId =
                await _groupService.IsSalesManager(_workContext.CurrentCustomer) ? _workContext.CurrentCustomer.SeId : "";

            var customers = await _customerService.GetAllCustomers(
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
                byte[] bytes = _exportManager.ExportCustomersToXlsx(customers);
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
                var ids = selectedIds
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x)
                    .ToArray();
                customers.AddRange(await _customerService.GetCustomersByIds(ids));
            }

            byte[] bytes = _exportManager.ExportCustomersToXlsx(customers);
            return File(bytes, "text/xls", "customers.xlsx");
        }

        #endregion
    }
}
