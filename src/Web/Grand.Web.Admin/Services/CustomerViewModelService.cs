using Grand.Business.Authentication.Interfaces;
using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Addresses;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Customers.Extensions;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Marketing.Interfaces.Contacts;
using Grand.Business.Marketing.Interfaces.Customers;
using Grand.Business.Marketing.Interfaces.Newsletters;
using Grand.Business.Messages.Interfaces;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Messages;
using Grand.Domain.Orders;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.SharedKernel;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Catalog;
using Grand.Web.Admin.Models.Common;
using Grand.Web.Admin.Models.Customers;
using Grand.Web.Admin.Models.Messages;
using Grand.Web.Admin.Models.ShoppingCart;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Services
{
    public partial class CustomerViewModelService : ICustomerViewModelService
    {
        private readonly ICustomerService _customerService;
        private readonly IGroupService _groupService;
        private readonly ICustomerProductService _customerProductService;
        private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
        private readonly IUserFieldService _userFieldService;
        private readonly ICustomerManagerService _customerManagerService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ITranslationService _translationService;
        private readonly ILoyaltyPointsService _loyaltyPointsService;
        private readonly ICountryService _countryService;
        private readonly IWorkContext _workContext;
        private readonly IVendorService _vendorService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IStoreService _storeService;
        private readonly ICustomerAttributeParser _customerAttributeParser;
        private readonly ICustomerAttributeService _customerAttributeService;
        private readonly IAddressAttributeParser _addressAttributeParser;
        private readonly IAddressAttributeService _addressAttributeService;
        private readonly IAffiliateService _affiliateService;
        private readonly ICustomerTagService _customerTagService;
        private readonly IProductService _productService;
        private readonly ISalesEmployeeService _salesEmployeeService;
        private readonly ICustomerNoteService _customerNoteService;
        private readonly IServiceProvider _serviceProvider;

        private readonly TaxSettings _taxSettings;
        private readonly LoyaltyPointsSettings _loyaltyPointsSettings;
        private readonly CustomerSettings _customerSettings;
        private readonly AddressSettings _addressSettings;
        private readonly CommonSettings _commonSettings;

        public CustomerViewModelService(
            ICustomerService customerService,
            IGroupService groupService,
            ICustomerProductService customerProductService,
            INewsLetterSubscriptionService newsLetterSubscriptionService,
            IUserFieldService userFieldService,
            ICustomerManagerService customerManagerService,
            IDateTimeService dateTimeService,
            ITranslationService translationService,
            ILoyaltyPointsService loyaltyPointsService,
            ICountryService countryService,
            IWorkContext workContext,
            IVendorService vendorService,
            ICustomerActivityService customerActivityService,
            IStoreService storeService,
            ICustomerAttributeParser customerAttributeParser,
            ICustomerAttributeService customerAttributeService,
            IAddressAttributeParser addressAttributeParser,
            IAddressAttributeService addressAttributeService,
            IAffiliateService affiliateService,
            ICustomerTagService customerTagService,
            IProductService productService,
            ISalesEmployeeService salesEmployeeService,
            ICustomerNoteService customerNoteService,
            IServiceProvider serviceProvider,
            CustomerSettings customerSettings,
            TaxSettings taxSettings,
            LoyaltyPointsSettings loyaltyPointsSettings,
            AddressSettings addressSettings,
            CommonSettings commonSettings)
        {
            _customerService = customerService;
            _groupService = groupService;
            _customerProductService = customerProductService;
            _newsLetterSubscriptionService = newsLetterSubscriptionService;
            _userFieldService = userFieldService;
            _customerManagerService = customerManagerService;
            _dateTimeService = dateTimeService;
            _translationService = translationService;
            _loyaltyPointsService = loyaltyPointsService;
            _taxSettings = taxSettings;
            _loyaltyPointsSettings = loyaltyPointsSettings;
            _countryService = countryService;
            _customerSettings = customerSettings;
            _commonSettings = commonSettings;
            _workContext = workContext;
            _vendorService = vendorService;
            _customerActivityService = customerActivityService;
            _addressSettings = addressSettings;
            _storeService = storeService;
            _customerAttributeParser = customerAttributeParser;
            _customerAttributeService = customerAttributeService;
            _addressAttributeParser = addressAttributeParser;
            _addressAttributeService = addressAttributeService;
            _affiliateService = affiliateService;
            _customerTagService = customerTagService;
            _productService = productService;
            _salesEmployeeService = salesEmployeeService;
            _customerNoteService = customerNoteService;
            _serviceProvider = serviceProvider;
        }

        #region Utilities

        protected virtual string[] ParseCustomerTags(string customerTags)
        {
            var result = new List<string>();
            if (!String.IsNullOrWhiteSpace(customerTags))
            {
                string[] values = customerTags.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string val1 in values)
                    if (!String.IsNullOrEmpty(val1.Trim()))
                        result.Add(val1.Trim());
            }
            return result.ToArray();
        }

        protected virtual async Task SaveCustomerTags(Customer customer, string[] customerTags)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            //product tags
            var existingCustomerTags = customer.CustomerTags.ToList();
            var customerTagsToRemove = new List<CustomerTag>();
            foreach (var existingCustomerTag in existingCustomerTags)
            {
                bool found = false;
                var existingCustomerTagName = await _customerTagService.GetCustomerTagById(existingCustomerTag);
                foreach (string newCustomerTag in customerTags)
                {
                    if (existingCustomerTagName.Name.Equals(newCustomerTag, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    customerTagsToRemove.Add(existingCustomerTagName);
                    await _customerTagService.DeleteTagFromCustomer(existingCustomerTagName.Id, customer.Id);
                }
            }

            foreach (string customerTagName in customerTags)
            {
                CustomerTag customerTag;
                var customerTag2 = await _customerTagService.GetCustomerTagByName(customerTagName);
                if (customerTag2 == null)
                {
                    customerTag = new CustomerTag {
                        Name = customerTagName,
                    };
                    await _customerTagService.InsertCustomerTag(customerTag);
                }
                else
                {
                    customerTag = customerTag2;
                }
                if (!customer.CustomerTags.Contains(customerTag.Id))
                {
                    await _customerTagService.InsertTagToCustomer(customerTag.Id, customer.Id);
                }
            }
        }

        protected virtual string GetCustomerGroupsNames(IList<CustomerGroup> customerGroups, string separator = ",")
        {
            var sb = new StringBuilder();
            for (int i = 0; i < customerGroups.Count; i++)
            {
                sb.Append(customerGroups[i].Name);
                if (i != customerGroups.Count - 1)
                {
                    sb.Append(separator);
                    sb.Append(" ");
                }
            }
            return sb.ToString();
        }

        protected virtual async Task<IList<CustomerModel.AssociatedExternalAuthModel>> GetAssociatedExternalAuthRecords(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));
            var openAuthenticationService = _serviceProvider.GetRequiredService<IExternalAuthenticationService>();
            var result = new List<CustomerModel.AssociatedExternalAuthModel>();
            foreach (var record in await openAuthenticationService.GetExternalIdentifiers(customer))
            {
                var method = openAuthenticationService.LoadAuthenticationProviderBySystemName(record.ProviderSystemName);
                if (method == null)
                    continue;

                result.Add(new CustomerModel.AssociatedExternalAuthModel {
                    Id = record.Id,
                    Email = record.Email,
                    ExternalIdentifier = record.ExternalIdentifier,
                    AuthMethodName = method.FriendlyName
                });
            }

            return result;
        }

        protected virtual async Task<CustomerModel> PrepareCustomerModelForList(Customer customer)
        {
            return new CustomerModel {
                Id = customer.Id,
                Email = !string.IsNullOrEmpty(customer.Email) ? customer.Email : _translationService.GetResource("Admin.Customers.Guest"),
                Username = customer.Username,
                FullName = customer.GetFullName(),
                Company = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Company),
                Phone = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Phone),
                ZipPostalCode = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.ZipPostalCode),
                CustomerGroupNames = GetCustomerGroupsNames(await _groupService.GetAllByIds(customer.Groups.ToArray())),
                Active = customer.Active,
                CreatedOn = _dateTimeService.ConvertToUserTime(customer.CreatedOnUtc, DateTimeKind.Utc),
                LastActivityDate = _dateTimeService.ConvertToUserTime(customer.LastActivityDateUtc, DateTimeKind.Utc),
            };
        }

        protected virtual async Task PrepareSelesEmployeeModel(CustomerModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            model.AvailableSalesEmployees.Add(new SelectListItem {
                Text = _translationService.GetResource("Admin.Customers.Customers.Fields.SalesEmployee.None"),
                Value = ""
            });
            var employees = await _salesEmployeeService.GetAll();
            foreach (var employee in employees.Where(x => x.Active))
            {
                model.AvailableSalesEmployees.Add(new SelectListItem {
                    Text = employee.Name,
                    Value = employee.Id
                });
            }
        }


        protected virtual async Task PrepareStoresModel(CustomerModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            model.AvailableStores.Add(new SelectListItem {
                Text = _translationService.GetResource("Admin.Customers.Customers.Fields.StaffStore.None"),
                Value = ""
            });
            var stores = await _storeService.GetAllStores();
            foreach (var store in stores)
            {
                model.AvailableStores.Add(new SelectListItem {
                    Text = store.Shortcut,
                    Value = store.Id.ToString()
                });
            }
        }

        protected virtual async Task PrepareCustomerAttributeModel(CustomerModel model, Customer customer)
        {
            var customerAttributes = await _customerAttributeService.GetAllCustomerAttributes();
            foreach (var attribute in customerAttributes)
            {
                var attributeModel = new CustomerModel.CustomerAttributeModel {
                    Id = attribute.Id,
                    Name = attribute.Name,
                    IsRequired = attribute.IsRequired,
                    AttributeControlType = attribute.AttributeControlTypeId,
                };

                if (attribute.ShouldHaveValues())
                {
                    //values
                    var attributeValues = attribute.CustomerAttributeValues;
                    foreach (var attributeValue in attributeValues)
                    {
                        var attributeValueModel = new CustomerModel.CustomerAttributeValueModel {
                            Id = attributeValue.Id,
                            Name = attributeValue.Name,
                            IsPreSelected = attributeValue.IsPreSelected
                        };
                        attributeModel.Values.Add(attributeValueModel);
                    }
                }


                //set already selected attributes
                if (customer != null)
                {
                    switch (attribute.AttributeControlTypeId)
                    {
                        case AttributeControlType.DropdownList:
                        case AttributeControlType.RadioList:
                        case AttributeControlType.Checkboxes:
                            {
                                if (customer.Attributes.Any())
                                {
                                    //clear default selection
                                    foreach (var item in attributeModel.Values)
                                        item.IsPreSelected = false;

                                    //select new values
                                    var selectedValues = await _customerAttributeParser.ParseCustomerAttributeValues(customer.Attributes);
                                    foreach (var attributeValue in selectedValues)
                                        if (attributeModel.Id == attributeValue.CustomerAttributeId)
                                            foreach (var item in attributeModel.Values)
                                                if (attributeValue.Id == item.Id)
                                                    item.IsPreSelected = true;
                                }
                            }
                            break;
                        case AttributeControlType.ReadonlyCheckboxes:
                            {
                                //do nothing
                                //values are already pre-set
                            }
                            break;
                        case AttributeControlType.TextBox:
                        case AttributeControlType.MultilineTextbox:
                            {
                                if (customer.Attributes.Any())
                                {
                                    var enteredText = customer.Attributes.Where(x => x.Key == attribute.Id).Select(x => x.Value).ToList();
                                    if (enteredText.Count > 0)
                                        attributeModel.DefaultValue = enteredText[0];
                                }
                            }
                            break;
                        case AttributeControlType.Datepicker:
                        case AttributeControlType.ColorSquares:
                        case AttributeControlType.ImageSquares:
                        case AttributeControlType.FileUpload:
                        default:
                            //not supported attribute control types
                            break;
                    }
                }

                model.CustomerAttributes.Add(attributeModel);
            }
        }

        #endregion

        public virtual async Task<CustomerListModel> PrepareCustomerListModel()
        {
            var registered = await _groupService.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Registered);
            var customerGroups = await _groupService.GetAllCustomerGroups(showHidden: true);
            var model = new CustomerListModel {
                UsernamesEnabled = _customerSettings.UsernamesEnabled,
                CompanyEnabled = _customerSettings.CompanyEnabled,
                PhoneEnabled = _customerSettings.PhoneEnabled,
                ZipPostalCodeEnabled = _customerSettings.ZipPostalCodeEnabled,
                AvailableCustomerGroups = customerGroups.Select(cr => new SelectListItem() { Text = cr.Name, Value = cr.Id.ToString(), Selected = (cr.Id == registered.Id) }).ToList(),
                AvailableCustomerTags = (await _customerTagService.GetAllCustomerTags()).Select(ct => new SelectListItem() { Text = ct.Name, Value = ct.Id.ToString() }).ToList(),
                SearchCustomerGroupIds = new List<string> { customerGroups.FirstOrDefault(x => x.Id == registered.Id).Id },
            };
            return model;
        }

        public virtual async Task<(IEnumerable<CustomerModel> customerModelList, int totalCount)> PrepareCustomerList(CustomerListModel model,
            string[] searchCustomerGroupIds, string[] searchCustomerTagIds, int pageIndex, int pageSize)
        {
            var salesEmployeeId = _workContext.CurrentCustomer.SeId;

            var customers = await _customerService.GetAllCustomers(
                customerGroupIds: searchCustomerGroupIds,
                customerTagIds: searchCustomerTagIds,
                email: model.SearchEmail,
                username: model.SearchUsername,
                firstName: model.SearchFirstName,
                lastName: model.SearchLastName,
                company: model.SearchCompany,
                phone: model.SearchPhone,
                zipPostalCode: model.SearchZipPostalCode,
                loadOnlyWithShoppingCart: false,
                salesEmployeeId: salesEmployeeId,
                pageIndex: pageIndex - 1,
                pageSize: pageSize);

            var customermodellist = new List<CustomerModel>();
            foreach (var item in customers)
            {
                customermodellist.Add(await PrepareCustomerModelForList(item));
            }
            return (customermodellist, customers.TotalCount);
        }

        public virtual async Task PrepareCustomerModel(CustomerModel model, Customer customer, bool excludeProperties)
        {
            var allStores = await _storeService.GetAllStores();
            if (customer != null)
            {
                model.Id = customer.Id;
                model.ShowMessageContactForm = _commonSettings.StoreInDatabaseContactUsForm;
                if (!excludeProperties)
                {
                    model.Email = customer.Email;
                    model.Username = customer.Username;
                    model.VendorId = customer.VendorId;
                    model.StaffStoreId = customer.StaffStoreId;
                    model.SeId = customer.SeId;
                    model.AdminComment = customer.AdminComment;
                    model.IsTaxExempt = customer.IsTaxExempt;
                    model.FreeShipping = customer.FreeShipping;
                    model.Active = customer.Active;
                    model.Owner = await _groupService.IsOwner(customer) ? "" : (await _customerService.GetCustomerById(customer.OwnerId))?.Email;
                    var result = new StringBuilder();
                    foreach (var item in customer.CustomerTags)
                    {
                        var ct = await _customerTagService.GetCustomerTagById(item);
                        result.Append(ct.Name);
                        result.Append(", ");
                    }
                    model.CustomerTags = result.ToString();
                    var affiliate = await _affiliateService.GetAffiliateById(customer.AffiliateId);
                    if (affiliate != null)
                    {
                        model.AffiliateId = affiliate.Id;
                        model.AffiliateName = affiliate.GetFullName();
                    }

                    model.VatNumber = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.VatNumber);
                    model.VatNumberStatusNote = ((VatNumberStatus)customer.GetUserFieldFromEntity<int>(SystemCustomerFieldNames.VatNumberStatusId))
                        .GetTranslationEnum(_translationService, _workContext);
                    model.CreatedOn = _dateTimeService.ConvertToUserTime(customer.CreatedOnUtc, DateTimeKind.Utc);
                    model.LastActivityDate = _dateTimeService.ConvertToUserTime(customer.LastActivityDateUtc, DateTimeKind.Utc);
                    if (customer.LastPurchaseDateUtc.HasValue)
                        model.LastPurchaseDate = _dateTimeService.ConvertToUserTime(customer.LastPurchaseDateUtc.Value, DateTimeKind.Utc);
                    model.LastIpAddress = customer.LastIpAddress;
                    model.UrlReferrer = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.UrlReferrer);
                    model.LastVisitedPage = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastVisitedPage);
                    model.LastUrlReferrer = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastUrlReferrer);

                    model.CustomerGroups = customer.Groups.ToArray();
                    //newsletter subscriptions
                    if (!String.IsNullOrEmpty(customer.Email))
                    {
                        var newsletterSubscriptionStoreIds = new List<string>();
                        foreach (var store in allStores)
                        {
                            var newsletterSubscription = await _newsLetterSubscriptionService
                                .GetNewsLetterSubscriptionByEmailAndStoreId(customer.Email, store.Id);
                            if (newsletterSubscription != null && newsletterSubscription.Active)
                                newsletterSubscriptionStoreIds.Add(store.Id);
                        }
                        model.SelectedNewsletterSubscriptionStoreIds = newsletterSubscriptionStoreIds.ToArray();
                    }


                    //form fields
                    model.FirstName = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.FirstName);
                    model.LastName = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastName);
                    model.Gender = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Gender);
                    model.DateOfBirth = customer.GetUserFieldFromEntity<DateTime?>(SystemCustomerFieldNames.DateOfBirth);
                    model.Company = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Company);
                    model.StreetAddress = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StreetAddress);
                    model.StreetAddress2 = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StreetAddress2);
                    model.ZipPostalCode = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.ZipPostalCode);
                    model.City = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.City);
                    model.CountryId = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.CountryId);
                    model.StateProvinceId = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StateProvinceId);
                    model.Phone = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Phone);
                    model.Fax = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Fax);
                }
            }
            else
            {
                model.SeId = _workContext.CurrentCustomer.SeId;
            }

            model.UsernamesEnabled = _customerSettings.UsernamesEnabled;
            model.AllowUsersToChangeUsernames = _customerSettings.AllowUsersToChangeUsernames;
            if (customer != null)
            {
                model.DisplayVatNumber = _taxSettings.EuVatEnabled;
            }
            else
            {
                model.DisplayVatNumber = false;
            }

            //stores
            await PrepareStoresModel(model);

            //employees
            await PrepareSelesEmployeeModel(model);

            //customer attributes
            await PrepareCustomerAttributeModel(model, customer);

            model.GenderEnabled = _customerSettings.GenderEnabled;
            model.DateOfBirthEnabled = _customerSettings.DateOfBirthEnabled;
            model.CompanyEnabled = _customerSettings.CompanyEnabled;
            model.StreetAddressEnabled = _customerSettings.StreetAddressEnabled;
            model.StreetAddress2Enabled = _customerSettings.StreetAddress2Enabled;
            model.ZipPostalCodeEnabled = _customerSettings.ZipPostalCodeEnabled;
            model.CityEnabled = _customerSettings.CityEnabled;
            model.CountryEnabled = _customerSettings.CountryEnabled;
            model.StateProvinceEnabled = _customerSettings.StateProvinceEnabled;
            model.PhoneEnabled = _customerSettings.PhoneEnabled;
            model.FaxEnabled = _customerSettings.FaxEnabled;

            //countries and states
            if (_customerSettings.CountryEnabled)
            {
                model.AvailableCountries.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
                foreach (var c in await _countryService.GetAllCountries(showHidden: true))
                {
                    model.AvailableCountries.Add(new SelectListItem {
                        Text = c.Name,
                        Value = c.Id.ToString(),
                        Selected = c.Id == model.CountryId
                    });
                }

                if (_customerSettings.StateProvinceEnabled)
                {
                    //states
                    var states = (await _countryService.GetCountryById(model.CountryId))?.StateProvinces;
                    model.AvailableStates.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Address.SelectState"), Value = "" });
                    if (states != null)
                        foreach (var s in states)
                        {
                            model.AvailableStates.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString(), Selected = (s.Id == model.StateProvinceId) });
                        }
                }
            }

            //newsletter subscriptions
            model.AvailableNewsletterSubscriptionStores = allStores
                .Select(s => new StoreModel() { Id = s.Id, Name = s.Shortcut })
                .ToList();


            if (customer != null)
            {
                //loyalty points history
                model.DisplayLoyaltyPointsHistory = _loyaltyPointsSettings.Enabled;
                model.AddLoyaltyPointsValue = 0;
                model.AddLoyaltyPointsMessage = "Some comment here...";

                //stores
                foreach (var store in allStores)
                {
                    model.LoyaltyPointsAvailableStores.Add(new SelectListItem {
                        Text = store.Shortcut,
                        Value = store.Id.ToString(),
                        Selected = (store.Id == _workContext.CurrentStore.Id)
                    });
                }

                //external authentication records
                model.AssociatedExternalAuthRecords = await GetAssociatedExternalAuthRecords(customer);

            }
            else
            {
                model.DisplayLoyaltyPointsHistory = false;
            }

            //sending of the welcome message:
            //1. "admin approval" registration method
            //2. already created customer
            //3. registered
            model.AllowSendingOfWelcomeMessage = _customerSettings.UserRegistrationType == UserRegistrationType.AdminApproval &&
                customer != null &&
                !string.IsNullOrEmpty(customer.Email);
            //sending of the activation message
            //1. "email validation" registration method
            //2. already created customer
            //3. has email
            //4. not active
            model.AllowReSendingOfActivationMessage = _customerSettings.UserRegistrationType == UserRegistrationType.EmailValidation &&
                customer != null &&
                !string.IsNullOrEmpty(customer.Email) &&
                !customer.Active;
        }

        public virtual async Task<string> ValidateCustomerGroups(IList<CustomerGroup> customerGroups)
        {
            if (customerGroups == null)
                throw new ArgumentNullException(nameof(customerGroups));

            //ensure a customer is not added to both 'Guests' and 'Registered' customer groups
            //ensure that a customer is in at least one required role ('Guests' and 'Registered')
            var isInGuestsGroup = customerGroups.FirstOrDefault(cr => cr.SystemName == SystemCustomerGroupNames.Guests) != null;
            var isInRegisteredGroup = customerGroups.FirstOrDefault(cr => cr.SystemName == SystemCustomerGroupNames.Registered) != null;
            var isAdminGroup = customerGroups.FirstOrDefault(cr => cr.SystemName == SystemCustomerGroupNames.Administrators) != null;

            if (isInGuestsGroup && isInRegisteredGroup)
                return "The customer cannot be in both 'Guests' and 'Registered' customer groups";

            if (!isInGuestsGroup && !isInRegisteredGroup)
                return "Add the customer to 'Guests' or 'Registered' customer group";

            if (await _groupService.IsSalesManager(_workContext.CurrentCustomer) && ((isInGuestsGroup && !isInRegisteredGroup) || customerGroups.Count != 1))
                return "Sales manager can assign role 'Registered' only";

            if (!await _groupService.IsAdmin(_workContext.CurrentCustomer) && isAdminGroup)
                return "Only administrators can assign role 'Administrators'";

            //no errors
            return "";
        }
        public virtual async Task<Customer> InsertCustomerModel(CustomerModel model)
        {
            var ownerId = string.Empty;
            if (!string.IsNullOrEmpty(model.Owner))
            {
                var customerOwner = await _customerService.GetCustomerByEmail(model.Owner);
                if (customerOwner != null)
                {
                    ownerId = customerOwner.Id;
                }
            }
            var customer = new Customer {
                CustomerGuid = Guid.NewGuid(),
                Email = model.Email,
                Username = model.Username,
                VendorId = model.VendorId,
                StaffStoreId = model.StaffStoreId,
                SeId = model.SeId,
                AdminComment = model.AdminComment,
                IsTaxExempt = model.IsTaxExempt,
                FreeShipping = model.FreeShipping,
                Active = model.Active,
                StoreId = _workContext.CurrentStore.Id,
                OwnerId = ownerId,
                Attributes = model.Attributes,
                CreatedOnUtc = DateTime.UtcNow,
                LastActivityDateUtc = DateTime.UtcNow,
            };

            //user fields
            customer.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.FirstName, Value = model.FirstName, StoreId = "" });
            customer.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.LastName, Value = model.LastName, StoreId = "" });

            if (_customerSettings.GenderEnabled)
                customer.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.Gender, Value = model.Gender, StoreId = "" });

            if (_customerSettings.DateOfBirthEnabled && model.DateOfBirth.HasValue)
                customer.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.DateOfBirth, Value = model.DateOfBirth.ToString(), StoreId = "" });
            if (_customerSettings.CompanyEnabled)
                customer.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.Company, Value = model.Company, StoreId = "" });
            if (_customerSettings.StreetAddressEnabled)
                customer.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.StreetAddress, Value = model.StreetAddress, StoreId = "" });
            if (_customerSettings.StreetAddress2Enabled)
                customer.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.StreetAddress2, Value = model.StreetAddress2, StoreId = "" });
            if (_customerSettings.ZipPostalCodeEnabled)
                customer.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.ZipPostalCode, Value = model.ZipPostalCode, StoreId = "" });
            if (_customerSettings.CityEnabled)
                customer.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.City, Value = model.City, StoreId = "" });
            if (_customerSettings.CountryEnabled)
                customer.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.CountryId, Value = model.CountryId, StoreId = "" });
            if (_customerSettings.CountryEnabled && _customerSettings.StateProvinceEnabled)
                customer.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.StateProvinceId, Value = model.StateProvinceId, StoreId = "" });
            if (_customerSettings.PhoneEnabled)
                customer.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.Phone, Value = model.Phone, StoreId = "" });
            if (_customerSettings.FaxEnabled)
                customer.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.Fax, Value = model.Fax, StoreId = "" });

            await _customerService.InsertCustomer(customer);

            //newsletter subscriptions
            if (!string.IsNullOrEmpty(customer.Email))
            {
                var allStores = await _storeService.GetAllStores();
                foreach (var store in allStores)
                {
                    var newsletterSubscription = await _newsLetterSubscriptionService
                        .GetNewsLetterSubscriptionByEmailAndStoreId(customer.Email, store.Id);
                    if (model.SelectedNewsletterSubscriptionStoreIds != null &&
                        model.SelectedNewsletterSubscriptionStoreIds.Contains(store.Id))
                    {
                        //subscribed
                        if (newsletterSubscription == null)
                        {
                            await _newsLetterSubscriptionService.InsertNewsLetterSubscription(new NewsLetterSubscription {
                                NewsLetterSubscriptionGuid = Guid.NewGuid(),
                                CustomerId = customer.Id,
                                Email = customer.Email,
                                Active = true,
                                StoreId = store.Id,
                                CreatedOnUtc = DateTime.UtcNow
                            });
                        }
                    }
                    else
                    {
                        //not subscribed
                        if (newsletterSubscription != null)
                        {
                            await _newsLetterSubscriptionService.DeleteNewsLetterSubscription(newsletterSubscription);
                        }
                    }
                }
            }

            var allCustomerGroups = await _groupService.GetAllCustomerGroups(showHidden: true);
            var newCustomerGroups = new List<CustomerGroup>();
            foreach (var customerGroup in allCustomerGroups)
                if (model.CustomerGroups != null && model.CustomerGroups.Contains(customerGroup.Id))
                    newCustomerGroups.Add(customerGroup);

            //customer groups
            foreach (var customerGroup in newCustomerGroups)
            {
                customer.Groups.Add(customerGroup.Id);
                await _customerService.InsertCustomerGroupInCustomer(customerGroup, customer.Id);
            }


            //ensure that a customer with a vendor associated is not in "Administrators" role
            //otherwise, he won't be have access to the other functionality in admin area
            if (await _groupService.IsAdmin(customer) && !String.IsNullOrEmpty(customer.VendorId))
            {
                customer.VendorId = "";
                await _customerService.UpdateCustomerField(customer.Id, x => x.VendorId, customer.VendorId);
            }

            //ensure that a customer in the Vendors role has a vendor account associated.
            //otherwise, he will have access to ALL products
            if (await _groupService.IsVendor(customer) && string.IsNullOrEmpty(customer.VendorId))
            {
                var customerGroups = await _groupService.GetAllByIds(customer.Groups.ToArray());
                var vendorGroup = customerGroups.FirstOrDefault(x => x.SystemName == SystemCustomerGroupNames.Vendors);
                customer.Groups.Remove(vendorGroup.Id);
                await _customerService.DeleteCustomerGroupInCustomer(vendorGroup, customer.Id);
            }

            //ensure that a customer in the Staff role has a staff account associated.
            //otherwise, he will have access to ALL products
            if (await _groupService.IsStaff(customer) && string.IsNullOrEmpty(customer.StaffStoreId))
            {
                var customerGroups = await _groupService.GetAllByIds(customer.Groups.ToArray());
                var staffGroup = customerGroups.FirstOrDefault(x => x.SystemName == SystemCustomerGroupNames.Staff);
                customer.Groups.Remove(staffGroup.Id);
                await _customerService.DeleteCustomerGroupInCustomer(staffGroup, customer.Id);
            }

            //ensure that a customer in the Sales manager role has a staff employee associated.
            //otherwise, he will have access to ALL customers
            if (await _groupService.IsSalesManager(customer) && string.IsNullOrEmpty(customer.SeId))
            {
                var customerGroups = await _groupService.GetAllByIds(customer.Groups.ToArray());
                var salesGroup = customerGroups.FirstOrDefault(x => x.SystemName == SystemCustomerGroupNames.SalesManager);
                customer.Groups.Remove(salesGroup.Id);
                await _customerService.DeleteCustomerGroupInCustomer(salesGroup, customer.Id);
            }

            //tags
            await SaveCustomerTags(customer, ParseCustomerTags(model.CustomerTags));

            //activity log
            await _customerActivityService.InsertActivity("AddNewCustomer", customer.Id, _translationService.GetResource("ActivityLog.AddNewCustomer"), customer.Id);

            return customer;
        }

        public virtual async Task<Customer> UpdateCustomerModel(Customer customer, CustomerModel model)
        {
            customer.AdminComment = model.AdminComment;
            customer.IsTaxExempt = model.IsTaxExempt;
            customer.FreeShipping = model.FreeShipping;
            customer.Active = model.Active;
            customer.Attributes = model.Attributes;
            //email
            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                await _customerManagerService.SetEmail(customer, model.Email);
            }
            else
            {
                customer.Email = model.Email;
            }

            //username
            if (_customerSettings.UsernamesEnabled && _customerSettings.AllowUsersToChangeUsernames)
            {
                if (!string.IsNullOrWhiteSpace(model.Username))
                {
                    await _customerManagerService.SetUsername(customer, model.Username);
                }
                else
                {
                    customer.Username = model.Username;
                }
            }

            if (!_customerSettings.UsernamesEnabled)
                customer.Username = model.Email;

            if (!string.IsNullOrEmpty(model.Owner))
            {
                var customerOwner = await _customerService.GetCustomerByEmail(model.Owner);
                if (customerOwner != null)
                {
                    customer.OwnerId = customerOwner.Id;
                }
            }
            else
                customer.OwnerId = string.Empty;

            //VAT number
            if (_taxSettings.EuVatEnabled)
            {
                var prevVatNumber = await customer.GetUserField<string>(_userFieldService, SystemCustomerFieldNames.VatNumber);

                await _userFieldService.SaveField(customer, SystemCustomerFieldNames.VatNumber, model.VatNumber);
                //set VAT number status
                if (!String.IsNullOrEmpty(model.VatNumber))
                {
                    if (!model.VatNumber.Equals(prevVatNumber, StringComparison.OrdinalIgnoreCase))
                    {
                        var checkVatService = _serviceProvider.GetRequiredService<IVatService>();
                        await _userFieldService.SaveField(customer,
                            SystemCustomerFieldNames.VatNumberStatusId,
                            (int)(await checkVatService.GetVatNumberStatus(model.VatNumber)).status);
                    }
                }
                else
                {
                    await _userFieldService.SaveField(customer,
                        SystemCustomerFieldNames.VatNumberStatusId,
                        (int)VatNumberStatus.Empty);
                }
            }

            //vendor
            customer.VendorId = model.VendorId;

            //staff store
            customer.StaffStoreId = model.StaffStoreId;

            //sales employee
            customer.SeId = model.SeId;

            //user fields
            if (_customerSettings.GenderEnabled)
                await _userFieldService.SaveField(customer, SystemCustomerFieldNames.Gender, model.Gender);
            await _userFieldService.SaveField(customer, SystemCustomerFieldNames.FirstName, model.FirstName);
            await _userFieldService.SaveField(customer, SystemCustomerFieldNames.LastName, model.LastName);
            if (_customerSettings.DateOfBirthEnabled)
                await _userFieldService.SaveField(customer, SystemCustomerFieldNames.DateOfBirth, model.DateOfBirth);
            if (_customerSettings.CompanyEnabled)
                await _userFieldService.SaveField(customer, SystemCustomerFieldNames.Company, model.Company);
            if (_customerSettings.StreetAddressEnabled)
                await _userFieldService.SaveField(customer, SystemCustomerFieldNames.StreetAddress, model.StreetAddress);
            if (_customerSettings.StreetAddress2Enabled)
                await _userFieldService.SaveField(customer, SystemCustomerFieldNames.StreetAddress2, model.StreetAddress2);
            if (_customerSettings.ZipPostalCodeEnabled)
                await _userFieldService.SaveField(customer, SystemCustomerFieldNames.ZipPostalCode, model.ZipPostalCode);
            if (_customerSettings.CityEnabled)
                await _userFieldService.SaveField(customer, SystemCustomerFieldNames.City, model.City);
            if (_customerSettings.CountryEnabled)
                await _userFieldService.SaveField(customer, SystemCustomerFieldNames.CountryId, model.CountryId);
            if (_customerSettings.CountryEnabled && _customerSettings.StateProvinceEnabled)
                await _userFieldService.SaveField(customer, SystemCustomerFieldNames.StateProvinceId, model.StateProvinceId);
            if (_customerSettings.PhoneEnabled)
                await _userFieldService.SaveField(customer, SystemCustomerFieldNames.Phone, model.Phone);
            if (_customerSettings.FaxEnabled)
                await _userFieldService.SaveField(customer, SystemCustomerFieldNames.Fax, model.Fax);

            //newsletter subscriptions
            if (!string.IsNullOrEmpty(customer.Email))
            {
                var allStores = await _storeService.GetAllStores();
                foreach (var store in allStores)
                {
                    var newsletterSubscription = await _newsLetterSubscriptionService
                        .GetNewsLetterSubscriptionByEmailAndStoreId(customer.Email, store.Id);
                    if (model.SelectedNewsletterSubscriptionStoreIds != null &&
                        model.SelectedNewsletterSubscriptionStoreIds.Contains(store.Id))
                    {
                        //subscribed
                        if (newsletterSubscription == null)
                        {
                            await _newsLetterSubscriptionService.InsertNewsLetterSubscription(new NewsLetterSubscription {
                                NewsLetterSubscriptionGuid = Guid.NewGuid(),
                                CustomerId = customer.Id,
                                Email = customer.Email,
                                Active = true,
                                StoreId = store.Id,
                                CreatedOnUtc = DateTime.UtcNow
                            });
                        }
                    }
                    else
                    {
                        //not subscribed
                        if (newsletterSubscription != null)
                        {
                            await _newsLetterSubscriptionService.DeleteNewsLetterSubscription(newsletterSubscription);
                        }
                    }
                }
            }
            var allCustomerGroups = await _groupService.GetAllCustomerGroups(showHidden: true);

            //customer groups
            foreach (var customerGroup in allCustomerGroups)
            {
                if (model.CustomerGroups != null &&
                model.CustomerGroups.Contains(customerGroup.Id))
                {
                    //new role
                    if (customer.Groups.Count(cr => cr == customerGroup.Id) == 0)
                    {
                        customer.Groups.Add(customerGroup.Id);
                    }
                }
                else
                {
                    //remove role
                    if (customer.Groups.Count(cr => cr == customerGroup.Id) > 0)
                        customer.Groups.Remove(customer.Groups.First(x => x == customerGroup.Id));
                }
            }
            await _customerService.UpdateCustomerinAdminPanel(customer);


            //ensure that a customer with a vendor associated is not in "Administrators" role
            //otherwise, he won't have access to the other functionality in admin area
            if (await _groupService.IsAdmin(customer) && !string.IsNullOrEmpty(customer.VendorId))
            {
                customer.VendorId = "";
                await _customerService.UpdateCustomerinAdminPanel(customer);
            }

            //ensure that a customer with a staff associated is not in "Administrators" role
            //otherwise, he won't have access to the other functionality in admin area
            if (await _groupService.IsAdmin(customer) && !string.IsNullOrEmpty(customer.StaffStoreId))
            {
                customer.StaffStoreId = "";
                await _customerService.UpdateCustomerinAdminPanel(customer);
            }

            //ensure that a customer in the Vendors role has a vendor account associated.
            //otherwise, he will have access to ALL products
            if (await _groupService.IsVendor(customer) && string.IsNullOrEmpty(customer.VendorId))
            {
                var customerGroups = await _groupService.GetAllByIds(customer.Groups.ToArray());
                var vendorGroup = customerGroups.FirstOrDefault(x => x.SystemName == SystemCustomerGroupNames.Vendors);
                customer.Groups.Remove(vendorGroup.Id);
                await _customerService.DeleteCustomerGroupInCustomer(vendorGroup, customer.Id);
            }

            //ensure that a customer in the Sales manager role has a staff employee associated.
            //otherwise, he will have access to ALL customers
            if (await _groupService.IsSalesManager(customer) && string.IsNullOrEmpty(customer.SeId))
            {
                var customerGroups = await _groupService.GetAllByIds(customer.Groups.ToArray());
                var salesGroup = customerGroups.FirstOrDefault(x => x.SystemName == SystemCustomerGroupNames.SalesManager);
                customer.Groups.Remove(salesGroup.Id);
                await _customerService.DeleteCustomerGroupInCustomer(salesGroup, customer.Id);
            }

            //ensure that a customer in the Staff group has a staff account associated.
            //otherwise, he will have access to ALL products
            if (await _groupService.IsStaff(customer) && string.IsNullOrEmpty(customer.StaffStoreId))
            {
                var customerGroups = await _groupService.GetAllByIds(customer.Groups.ToArray());
                var staffGroup = customerGroups.FirstOrDefault(x => x.SystemName == SystemCustomerGroupNames.Staff);
                customer.Groups.Remove(staffGroup.Id);
                await _customerService.DeleteCustomerGroupInCustomer(staffGroup, customer.Id);
            }

            //tags
            await SaveCustomerTags(customer, ParseCustomerTags(model.CustomerTags));

            //activity log
            await _customerActivityService.InsertActivity("EditCustomer", customer.Id, _translationService.GetResource("ActivityLog.EditCustomer"), customer.Id);
            return customer;
        }

        public virtual async Task DeleteCustomer(Customer customer)
        {
            await _customerService.DeleteCustomer(customer);

            //remove newsletter subscription (if exists)
            foreach (var store in await _storeService.GetAllStores())
            {
                var subscription = await _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreId(customer.Email, store.Id);
                if (subscription != null)
                    await _newsLetterSubscriptionService.DeleteNewsLetterSubscription(subscription);
            }

            //activity log
            await _customerActivityService.InsertActivity("DeleteCustomer", customer.Id, _translationService.GetResource("ActivityLog.DeleteCustomer"), customer.Id);
        }

        public virtual async Task DeleteSelected(IList<string> selectedIds)
        {
            var customers = new List<Customer>();
            customers.AddRange(await _customerService.GetCustomersByIds(selectedIds.ToArray()));
            for (var i = 0; i < customers.Count; i++)
            {
                var customer = customers[i];
                if (customer.Id != _workContext.CurrentCustomer.Id)
                {
                    await _customerService.DeleteCustomer(customer);
                }
                //activity log
                await _customerActivityService.InsertActivity("DeleteCustomer", customer.Id, _translationService.GetResource("ActivityLog.DeleteCustomer"), customer.Id);
            }
        }

        public async Task SendEmail(Customer customer, CustomerModel.SendEmailModel model)
        {
            var emailAccountService = _serviceProvider.GetRequiredService<IEmailAccountService>();
            var emailAccountSettings = _serviceProvider.GetRequiredService<EmailAccountSettings>();
            var queuedEmailService = _serviceProvider.GetRequiredService<IQueuedEmailService>();

            var emailAccount = await emailAccountService.GetEmailAccountById(emailAccountSettings.DefaultEmailAccountId);
            if (emailAccount == null)
                emailAccount = (await emailAccountService.GetAllEmailAccounts()).FirstOrDefault();
            if (emailAccount == null)
                throw new GrandException("Email account can't be loaded");

            var email = new QueuedEmail {
                PriorityId = QueuedEmailPriority.High,
                EmailAccountId = emailAccount.Id,
                FromName = emailAccount.DisplayName,
                From = emailAccount.Email,
                ToName = customer.GetFullName(),
                To = customer.Email,
                Subject = model.Subject,
                Body = model.Body,
                CreatedOnUtc = DateTime.UtcNow,
                DontSendBeforeDateUtc = (model.SendImmediately || !model.DontSendBeforeDate.HasValue) ?
                        null : (DateTime?)_dateTimeService.ConvertToUtcTime(model.DontSendBeforeDate.Value)
            };
            await queuedEmailService.InsertQueuedEmail(email);
            await _customerActivityService.InsertActivity("CustomerAdmin.SendEmail", "", _translationService.GetResource("ActivityLog.SendEmailfromAdminPanel"), customer, model.Subject);
        }

        public virtual async Task<IEnumerable<CustomerModel.LoyaltyPointsHistoryModel>> PrepareLoyaltyPointsHistoryModel(string customerId)
        {
            var model = new List<CustomerModel.LoyaltyPointsHistoryModel>();
            foreach (var rph in await _loyaltyPointsService.GetLoyaltyPointsHistory(customerId, showAll: true))
            {
                var store = await _storeService.GetStoreById(rph.StoreId);
                model.Add(new CustomerModel.LoyaltyPointsHistoryModel {
                    StoreName = store != null ? store.Shortcut : "Unknown",
                    Points = rph.Points,
                    PointsBalance = rph.PointsBalance,
                    Message = rph.Message,
                    CreatedOn = _dateTimeService.ConvertToUserTime(rph.CreatedOnUtc, DateTimeKind.Utc)
                });
            }
            return model;
        }

        public virtual async Task<LoyaltyPointsHistory> InsertLoyaltyPointsHistory(Customer customer, string storeId, int addLoyaltyPointsValue, string addLoyaltyPointsMessage)
        {
            //activity log
            await _customerActivityService.InsertActivity("AddLoyaltyPoints", customer.Id, _translationService.GetResource("ActivityLog.AddNewLoyaltyPoints"), customer.Email, addLoyaltyPointsValue);

            return await _loyaltyPointsService.AddLoyaltyPointsHistory(customer.Id, addLoyaltyPointsValue, storeId, addLoyaltyPointsMessage);
        }

        public virtual async Task<IEnumerable<AddressModel>> PrepareAddressModel(Customer customer)
        {
            var addresses = customer.Addresses.OrderByDescending(a => a.CreatedOnUtc).ThenByDescending(a => a.Id).ToList();
            var addressesListModel = new List<AddressModel>();
            foreach (var x in addresses)
            {
                var model = await x.ToModel(_countryService);
                var addressHtmlSb = new StringBuilder("<div>");
                if (_addressSettings.CompanyEnabled && !String.IsNullOrEmpty(model.Company))
                    addressHtmlSb.AppendFormat("{0}<br />", WebUtility.HtmlEncode(model.Company));
                if (_addressSettings.StreetAddressEnabled && !String.IsNullOrEmpty(model.Address1))
                    addressHtmlSb.AppendFormat("{0}<br />", WebUtility.HtmlEncode(model.Address1));
                if (_addressSettings.StreetAddress2Enabled && !String.IsNullOrEmpty(model.Address2))
                    addressHtmlSb.AppendFormat("{0}<br />", WebUtility.HtmlEncode(model.Address2));
                if (_addressSettings.CityEnabled && !String.IsNullOrEmpty(model.City))
                    addressHtmlSb.AppendFormat("{0},", WebUtility.HtmlEncode(model.City));
                if (_addressSettings.StateProvinceEnabled && !String.IsNullOrEmpty(model.StateProvinceName))
                    addressHtmlSb.AppendFormat("{0},", WebUtility.HtmlEncode(model.StateProvinceName));
                if (_addressSettings.ZipPostalCodeEnabled && !String.IsNullOrEmpty(model.ZipPostalCode))
                    addressHtmlSb.AppendFormat("{0}<br />", WebUtility.HtmlEncode(model.ZipPostalCode));
                if (_addressSettings.CountryEnabled && !String.IsNullOrEmpty(model.CountryName))
                    addressHtmlSb.AppendFormat("{0}", WebUtility.HtmlEncode(model.CountryName));
                var customAttributesFormatted = await _addressAttributeParser.FormatAttributes(_workContext.WorkingLanguage, x.Attributes);
                if (!string.IsNullOrEmpty(customAttributesFormatted))
                {
                    //already encoded
                    addressHtmlSb.AppendFormat("<br />{0}", customAttributesFormatted);
                }
                addressHtmlSb.Append("</div>");
                model.AddressHtml = addressHtmlSb.ToString();
                addressesListModel.Add(model);
            }
            return addressesListModel;
        }
        public virtual async Task DeleteAddress(Customer customer, Address address)
        {
            customer.RemoveAddress(address);
            await _customerService.UpdateCustomerinAdminPanel(customer);
        }

        public virtual async Task<Address> InsertAddressModel(Customer customer, CustomerAddressModel model, List<CustomAttribute> customAttributes)
        {
            var address = model.Address.ToEntity();
            address.Attributes = customAttributes;
            address.CreatedOnUtc = DateTime.UtcNow;
            customer.Addresses.Add(address);
            await _customerService.UpdateCustomerinAdminPanel(customer);
            return address;
        }

        public virtual async Task PrepareAddressModel(CustomerAddressModel model, Address address, Customer customer, bool excludeProperties)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            model.CustomerId = customer.Id;
            if (address != null)
            {
                if (!excludeProperties)
                {
                    model.Address = await address.ToModel(_countryService);
                }
            }

            if (model.Address == null)
                model.Address = new AddressModel();

            model.Address.FirstNameEnabled = true;
            model.Address.FirstNameRequired = true;
            model.Address.LastNameEnabled = true;
            model.Address.LastNameRequired = true;
            model.Address.EmailEnabled = true;
            model.Address.EmailRequired = true;
            model.Address.CompanyEnabled = _addressSettings.CompanyEnabled;
            model.Address.CompanyRequired = _addressSettings.CompanyRequired;
            model.Address.VatNumberEnabled = _addressSettings.VatNumberEnabled;
            model.Address.VatNumberRequired = _addressSettings.VatNumberRequired;
            model.Address.CountryEnabled = _addressSettings.CountryEnabled;
            model.Address.StateProvinceEnabled = _addressSettings.StateProvinceEnabled;
            model.Address.CityEnabled = _addressSettings.CityEnabled;
            model.Address.CityRequired = _addressSettings.CityRequired;
            model.Address.StreetAddressEnabled = _addressSettings.StreetAddressEnabled;
            model.Address.StreetAddressRequired = _addressSettings.StreetAddressRequired;
            model.Address.StreetAddress2Enabled = _addressSettings.StreetAddress2Enabled;
            model.Address.StreetAddress2Required = _addressSettings.StreetAddress2Required;
            model.Address.ZipPostalCodeEnabled = _addressSettings.ZipPostalCodeEnabled;
            model.Address.ZipPostalCodeRequired = _addressSettings.ZipPostalCodeRequired;
            model.Address.PhoneEnabled = _addressSettings.PhoneEnabled;
            model.Address.PhoneRequired = _addressSettings.PhoneRequired;
            model.Address.FaxEnabled = _addressSettings.FaxEnabled;
            model.Address.FaxRequired = _addressSettings.FaxRequired;
            model.Address.NoteEnabled = _addressSettings.NoteEnabled;

            //countries
            model.Address.AvailableCountries.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
            foreach (var c in await _countryService.GetAllCountries(showHidden: true))
                model.Address.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString(), Selected = (c.Id == model.Address.CountryId) });
            //states
            var states = !String.IsNullOrEmpty(model.Address.CountryId) ? (await _countryService.GetCountryById(model.Address.CountryId))?.StateProvinces : new List<StateProvince>();
            if (states.Count > 0)
            {
                foreach (var s in states)
                    model.Address.AvailableStates.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString(), Selected = (s.Id == model.Address.StateProvinceId) });
            }

            //customer attribute services
            await model.Address.PrepareCustomAddressAttributes(address, _addressAttributeService, _addressAttributeParser);
        }

        public virtual async Task<Address> UpdateAddressModel(Customer customer, Address address, CustomerAddressModel model, List<CustomAttribute> customAttributes)
        {
            address = model.Address.ToEntity(address);
            address.Attributes = customAttributes;
            await _customerService.UpdateCustomerinAdminPanel(customer);
            return address;
        }

        public virtual async Task<IList<ShoppingCartItemModel>> PrepareShoppingCartItemModel(string customerId, int cartTypeId)
        {
            var customer = await _customerService.GetCustomerById(customerId);
            var cart = customer.ShoppingCartItems.Where(x => x.ShoppingCartTypeId == (ShoppingCartType)cartTypeId).ToList();
            var items = new List<ShoppingCartItemModel>();
            if (cart.Any())
            {
                var taxService = _serviceProvider.GetRequiredService<ITaxService>();
                var priceCalculationService = _serviceProvider.GetRequiredService<IPricingService>();
                var priceFormatter = _serviceProvider.GetRequiredService<IPriceFormatter>();

                foreach (var sci in cart)
                {
                    var store = await _storeService.GetStoreById(sci.StoreId);
                    var product = await _productService.GetProductById(sci.ProductId);
                    if (product != null)
                    {
                        var price = (await taxService.GetProductPrice(product, (await priceCalculationService.GetUnitPrice(sci, product)).unitprice)).productprice;
                        var sciModel = new ShoppingCartItemModel {
                            Id = sci.Id,
                            Store = store != null ? store.Shortcut : "Unknown",
                            ProductId = sci.ProductId,
                            Quantity = sci.Quantity,
                            ProductName = product.Name,
                            AttributeInfo = await _serviceProvider.GetRequiredService<IProductAttributeFormatter>().FormatAttributes(product, sci.Attributes),
                            UnitPrice = priceFormatter.FormatPrice(price),
                            UnitPriceValue = price,
                            Total = priceFormatter.FormatPrice((await taxService.GetProductPrice(product, (await priceCalculationService.GetSubTotal(sci, product)).subTotal)).productprice),
                            UpdatedOn = _dateTimeService.ConvertToUserTime(sci.UpdatedOnUtc, DateTimeKind.Utc)
                        };
                        items.Add(sciModel);
                    }
                }
            }
            return items;
        }
        public virtual async Task DeleteCart(Customer customer, string id)
        {
            var cart = customer.ShoppingCartItems.FirstOrDefault(a => a.Id == id);
            if (cart != null)
            {
                await _serviceProvider.GetRequiredService<IShoppingCartService>()
                    .DeleteShoppingCartItem(customer, cart, ensureOnlyActiveCheckoutAttributes: true);
                await _customerService.UpdateCustomerinAdminPanel(customer);
            }
        }

        public virtual async Task<IList<string>> UpdateCart(Customer customer, string shoppingCartId, double? unitprice)
        {
            var cart = customer.ShoppingCartItems.FirstOrDefault(a => a.Id == shoppingCartId);
            if (cart != null)
            {
                //activity log
                await _customerActivityService.InsertActivity("CustomerAdmin.UpdateCartCustomer", _workContext.CurrentCustomer.Id,
                    _translationService.GetResource("ActivityLog.UpdateCartCustomer"), customer.Email, customer.Id, unitprice);

                return await _serviceProvider.GetRequiredService<IShoppingCartService>()
                    .UpdateShoppingCartItem(
                    customer,
                    shoppingCartId,
                    cart.WarehouseId,
                    cart.Attributes,
                    unitprice,
                    cart.RentalStartDateUtc,
                    cart.RentalEndDateUtc,
                    cart.Quantity,
                    true,
                    cart.ReservationId,
                    shoppingCartId);
            }
            return new List<string>();
        }
        public virtual async Task<(IEnumerable<CustomerModel.ProductPriceModel> productPriceModels, int totalCount)> PrepareProductPriceModel(string customerId, int pageIndex, int pageSize)
        {
            var productPrices = await _customerProductService.GetProductsPriceByCustomer(customerId, pageIndex - 1, pageSize);
            var items = new List<CustomerModel.ProductPriceModel>();
            foreach (var x in productPrices)
            {
                var m = new CustomerModel.ProductPriceModel {
                    Id = x.Id,
                    Price = x.Price,
                    ProductId = x.ProductId,
                    ProductName = (await _productService.GetProductById(x.ProductId))?.Name
                };
                items.Add(m);
            }
            return (items, productPrices.TotalCount);
        }
        public virtual async Task<(IEnumerable<CustomerModel.ProductModel> productModels, int totalCount)> PreparePersonalizedProducts(string customerId, int pageIndex, int pageSize)
        {
            var products = await _customerProductService.GetProductsByCustomer(customerId, pageIndex - 1, pageSize);
            var items = new List<CustomerModel.ProductModel>();
            foreach (var x in products)
            {
                var m = new CustomerModel.ProductModel {
                    Id = x.Id,
                    DisplayOrder = x.DisplayOrder,
                    ProductId = x.ProductId,
                    ProductName = (await _productService.GetProductById(x.ProductId))?.Name
                };
                items.Add(m);
            }
            return (items, products.TotalCount);
        }
        public virtual async Task<CustomerModel.AddProductModel> PrepareCustomerModelAddProductModel()
        {
            var model = new CustomerModel.AddProductModel();

            //stores
            model.AvailableStores.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            foreach (var s in await _storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id.ToString() });

            //vendors
            model.AvailableVendors.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            foreach (var v in await _vendorService.GetAllVendors(showHidden: true))
                model.AvailableVendors.Add(new SelectListItem { Text = v.Name, Value = v.Id.ToString() });

            //product types
            model.AvailableProductTypes = ProductType.SimpleProduct.ToSelectList(_translationService, _workContext, false).ToList();
            model.AvailableProductTypes.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });

            return model;
        }

        public virtual async Task<(IList<ProductModel> products, int totalCount)> PrepareProductModel(CustomerModel.AddProductModel model, int pageIndex, int pageSize)
        {
            var products = await _productService.PrepareProductList(model.SearchCategoryId, model.SearchBrandId, model.SearchCollectionId, model.SearchStoreId, model.SearchVendorId, model.SearchProductTypeId, model.SearchProductName, pageIndex, pageSize);
            return (products.Select(x => x.ToModel(_dateTimeService)).ToList(), products.TotalCount);
        }

        public virtual async Task InsertCustomerAddProductModel(string customerId, bool personalized, CustomerModel.AddProductModel model)
        {
            foreach (string id in model.SelectedProductIds)
            {
                var product = await _productService.GetProductById(id);
                if (product != null)
                {
                    if (!personalized)
                    {
                        if (!(await _customerProductService.GetPriceByCustomerProduct(customerId, id)).HasValue)
                        {
                            await _customerProductService.InsertCustomerProductPrice(new CustomerProductPrice() { CustomerId = customerId, ProductId = id, Price = product.Price });
                        }
                    }
                    else
                    {
                        if (await _customerProductService.GetCustomerProduct(customerId, id) == null)
                        {
                            await _customerProductService.InsertCustomerProduct(new CustomerProduct() { CustomerId = customerId, ProductId = id, DisplayOrder = 0 });
                        }

                    }
                }
            }
        }
        public virtual async Task UpdateProductPrice(CustomerModel.ProductPriceModel model)
        {
            var productPrice = await _customerProductService.GetCustomerProductPriceById(model.Id);
            if (productPrice != null)
            {
                productPrice.Price = model.Price;
                await _customerProductService.UpdateCustomerProductPrice(productPrice);
            }
        }
        public virtual async Task DeleteProductPrice(string id)
        {
            var productPrice = await _customerProductService.GetCustomerProductPriceById(id);
            if (productPrice == null)
                throw new ArgumentException("No productPrice found with the specified id");

            await _customerProductService.DeleteCustomerProductPrice(productPrice);
        }
        public virtual async Task UpdatePersonalizedProduct(CustomerModel.ProductModel model)
        {
            var customerproduct = await _customerProductService.GetCustomerProduct(model.Id);
            if (customerproduct != null)
            {
                customerproduct.DisplayOrder = model.DisplayOrder;
                await _customerProductService.UpdateCustomerProduct(customerproduct);
            }
        }
        public virtual async Task DeletePersonalizedProduct(string id)
        {
            var customerproduct = await _customerProductService.GetCustomerProduct(id);
            if (customerproduct == null)
                throw new ArgumentException("No customerproduct found with the specified id");

            await _customerProductService.DeleteCustomerProduct(customerproduct);
        }
        public virtual async Task<(IEnumerable<CustomerModel.ActivityLogModel> activityLogModels, int totalCount)> PrepareActivityLogModel(string customerId, int pageIndex, int pageSize)
        {
            var activityLog = await _customerActivityService.GetAllActivities(null, null, null, customerId, "", null, pageIndex - 1, pageSize);
            var items = new List<CustomerModel.ActivityLogModel>();
            foreach (var x in activityLog)
            {
                var m = new CustomerModel.ActivityLogModel {
                    Id = x.Id,
                    ActivityLogTypeName = (await _customerActivityService.GetActivityTypeById(x.ActivityLogTypeId))?.Name,
                    Comment = x.Comment,
                    CreatedOn = _dateTimeService.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc),
                    IpAddress = x.IpAddress
                };
                items.Add(m);
            }
            return (items, activityLog.TotalCount);
        }
        public virtual async Task<(IEnumerable<ContactFormModel> contactFormModels, int totalCount)> PrepareContactFormModel(string customerId, string vendorId, int pageIndex, int pageSize)
        {
            var contactUsService = _serviceProvider.GetRequiredService<IContactUsService>();
            var contactform = await contactUsService.GetAllContactUs(storeId: "", vendorId: vendorId, customerId: customerId, pageIndex: pageIndex - 1, pageSize: pageSize);
            var items = new List<ContactFormModel>();
            foreach (var x in contactform)
            {
                var store = await _storeService.GetStoreById(x.StoreId);
                var m = x.ToModel();
                m.CreatedOn = _dateTimeService.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc);
                m.Enquiry = "";
                m.Email = m.FullName + " - " + m.Email;
                m.Store = store != null ? store.Shortcut : "-empty-";
                items.Add(m);
            }
            return (items, contactform.TotalCount);
        }
        public virtual async Task<(IEnumerable<CustomerModel.OutOfStockSubscriptionModel> outOfStockSubscriptionModels, int totalCount)> PrepareOutOfStockSubscriptionModel(string customerId, int pageIndex, int pageSize)
        {
            var outOfStockSubscriptionService = _serviceProvider.GetRequiredService<IOutOfStockSubscriptionService>();
            var subscriptions = await outOfStockSubscriptionService.GetAllSubscriptionsByCustomerId(customerId, "", pageIndex - 1, pageSize);
            var items = new List<CustomerModel.OutOfStockSubscriptionModel>();
            if (subscriptions.Any())
            {
                var productAttributeFormatter = _serviceProvider.GetRequiredService<IProductAttributeFormatter>();
                foreach (var x in subscriptions)
                {
                    var store = await _storeService.GetStoreById(x.StoreId);
                    var product = await _productService.GetProductById(x.ProductId);
                    var m = new CustomerModel.OutOfStockSubscriptionModel {
                        Id = x.Id,
                        StoreName = store != null ? store.Shortcut : "Unknown",
                        ProductId = x.ProductId,
                        ProductName = product != null ? product.Name : "Unknown",
                        AttributeDescription = x.Attributes != null || !x.Attributes.Any() ? "" : await productAttributeFormatter.FormatAttributes(product, x.Attributes),
                        CreatedOn = _dateTimeService.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc)
                    };
                    items.Add(m);
                }
            }
            return (items, subscriptions.TotalCount);
        }
        public virtual async Task<IList<CustomerModel.CustomerNote>> PrepareCustomerNoteList(string customerId)
        {
            var downloadService = _serviceProvider.GetRequiredService<IDownloadService>();
            var customerNoteModels = new List<CustomerModel.CustomerNote>();
            foreach (var customerNote in (await _customerNoteService.GetCustomerNotes(customerId))
                .OrderByDescending(on => on.CreatedOnUtc))
            {
                var download = !string.IsNullOrEmpty(customerNote.DownloadId) ? await downloadService.GetDownloadById(customerNote.DownloadId) : null;
                customerNoteModels.Add(new CustomerModel.CustomerNote {
                    Id = customerNote.Id,
                    CustomerId = customerId,
                    DownloadId = string.IsNullOrEmpty(customerNote.DownloadId) ? "" : customerNote.DownloadId,
                    DownloadGuid = download != null ? download.DownloadGuid : Guid.Empty,
                    DisplayToCustomer = customerNote.DisplayToCustomer,
                    Title = customerNote.Title,
                    Note = customerNote.Note,
                    CreatedOn = _dateTimeService.ConvertToUserTime(customerNote.CreatedOnUtc, DateTimeKind.Utc)
                });
            }
            return customerNoteModels;
        }
        public virtual async Task<CustomerNote> InsertCustomerNote(string customerId, string downloadId, bool displayToCustomer, string title, string message)
        {
            var customerNote = new CustomerNote {
                DisplayToCustomer = displayToCustomer,
                Title = title,
                Note = message,
                DownloadId = downloadId,
                CustomerId = customerId,
                CreatedOnUtc = DateTime.UtcNow,
            };
            await _customerNoteService.InsertCustomerNote(customerNote);

            //new customer note notification
            if (displayToCustomer)
            {
                //email
                var messageProviderService = _serviceProvider.GetRequiredService<IMessageProviderService>();
                await messageProviderService.SendNewCustomerNoteMessage(customerNote,
                    await _customerService.GetCustomerById(customerId), _workContext.CurrentStore, _workContext.WorkingLanguage.Id);

            }
            return customerNote;
        }
        public virtual async Task DeleteCustomerNote(string id, string customerId)
        {
            var customerNote = await _customerNoteService.GetCustomerNote(id);
            if (customerNote == null)
                throw new ArgumentException("No customer note found with the specified id");

            await _customerNoteService.DeleteCustomerNote(customerNote);
        }
    }
}
