using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Marketing.Customers;
using Grand.Business.Core.Interfaces.Marketing.Newsletters;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Messages;
using Grand.Domain.Orders;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.SharedKernel;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Models;
using Grand.Web.Common.Security.Oidc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace Grand.Web.AdminShared.Services;

public class CustomerViewModelService : ICustomerViewModelService
{
    private readonly IAffiliateService _affiliateService;
    private readonly CommonSettings _commonSettings;
    private readonly ICountryService _countryService;
    private readonly ICustomerAttributeParser _customerAttributeParser;
    private readonly ICustomerAttributeService _customerAttributeService;
    private readonly ICustomerService _customerService;
    private readonly CustomerSettings _customerSettings;
    private readonly ICustomerTagService _customerTagService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IGroupService _groupService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LoyaltyPointsSettings _loyaltyPointsSettings;
    private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
    private readonly ISalesEmployeeService _salesEmployeeService;
    private readonly IStoreService _storeService;

    private readonly TaxSettings _taxSettings;
    private readonly IRequestTranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;
    private readonly IEnumTranslationService _enumTranslationService;
    private readonly CustomerConfig _customerConfig;
    private readonly IAdminDataScope<Customer> _scope;
    private readonly IPriceFormatter _priceFormatter;
    private readonly ICurrencyService _currencyService;
    private readonly ICompanyCreditService _companyCreditService;

    public CustomerViewModelService(
        ICustomerService customerService,
        IGroupService groupService,
        INewsLetterSubscriptionService newsLetterSubscriptionService,
        IDateTimeService dateTimeService,
        IRequestTranslationService translationService,
        ICountryService countryService,
        IContextAccessor contextAccessor,
        IStoreService storeService,
        ICustomerAttributeParser customerAttributeParser,
        ICustomerAttributeService customerAttributeService,
        IAffiliateService affiliateService,
        ICustomerTagService customerTagService,
        ISalesEmployeeService salesEmployeeService,
        IHttpContextAccessor httpContextAccessor,
        CustomerSettings customerSettings,
        TaxSettings taxSettings,
        LoyaltyPointsSettings loyaltyPointsSettings,
        CommonSettings commonSettings,
        IEnumTranslationService enumTranslationService,
        CustomerConfig customerConfig,
        IAdminDataScope<Customer> scope,
        IPriceFormatter priceFormatter,
        ICurrencyService currencyService,
        ICompanyCreditService companyCreditService)
    {
        _companyCreditService = companyCreditService;
        _priceFormatter = priceFormatter;
        _currencyService = currencyService;
        _customerConfig = customerConfig;
        _scope = scope;
        _customerService = customerService;
        _groupService = groupService;
        _newsLetterSubscriptionService = newsLetterSubscriptionService;
        _dateTimeService = dateTimeService;
        _translationService = translationService;
        _taxSettings = taxSettings;
        _loyaltyPointsSettings = loyaltyPointsSettings;
        _countryService = countryService;
        _customerSettings = customerSettings;
        _commonSettings = commonSettings;
        _enumTranslationService = enumTranslationService;
        _contextAccessor = contextAccessor;
        _storeService = storeService;
        _customerAttributeParser = customerAttributeParser;
        _customerAttributeService = customerAttributeService;
        _affiliateService = affiliateService;
        _customerTagService = customerTagService;
        _salesEmployeeService = salesEmployeeService;
        _httpContextAccessor = httpContextAccessor;
    }

    public virtual async Task<CustomerListModel> PrepareCustomerListModel()
    {
        var registered = await _groupService.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Registered);
        var customerGroups = await _groupService.GetAllCustomerGroups(showHidden: true);
        var model = new CustomerListModel {
            UsernamesEnabled = _customerSettings.UsernamesEnabled,
            CompanyEnabled = _customerSettings.CompanyEnabled,
            PhoneEnabled = _customerSettings.PhoneEnabled,
            ZipPostalCodeEnabled = _customerSettings.ZipPostalCodeEnabled,
            AvailableCustomerGroups = customerGroups.Select(cr => new SelectListItem
                { Text = cr.Name, Value = cr.Id.ToString(), Selected = cr.Id == registered.Id }).ToList(),
            AvailableCustomerTags = (await _customerTagService.GetAllCustomerTags())
                .Select(ct => new SelectListItem { Text = ct.Name, Value = ct.Id.ToString() }).ToList(),
            SearchCustomerGroupIds = new List<string> { customerGroups.FirstOrDefault(x => x.Id == registered.Id)?.Id }
        };
        return model;
    }

    public virtual async Task<(IEnumerable<CustomerModel> customerModelList, int totalCount)> PrepareCustomerList(
        CustomerListModel model,
        string[] searchCustomerGroupIds, string[] searchCustomerTagIds, int pageIndex, int pageSize,
        string storeId = "")
    {
        var salesEmployeeId = _contextAccessor.WorkContext.CurrentCustomer.SeId;

        //the Store panel lists its store's customers, never the staff (see StoreCustomerDataScope); left out in the
        //query, so paging and the total stay right
        var staffExcluded = await _scope.GetDefaultStoreIdAsync() is not null;
        var excludedGroupIds = new List<string>();
        if (staffExcluded)
            foreach (var systemName in StoreCustomerDataScope.StaffGroupSystemNames)
            {
                var group = await _groupService.GetCustomerGroupBySystemName(systemName);
                if (group != null) excludedGroupIds.Add(group.Id);
            }

        var customers = await _customerService.GetAllCustomers(
            excludedCustomerGroupIds: staffExcluded ? excludedGroupIds.ToArray() : null,
            excludeVendorAndStaffAccounts: staffExcluded,
            storeId: storeId,
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
        foreach (var item in customers) customermodellist.Add(await PrepareCustomerModelForList(item));
        return (customermodellist, customers.TotalCount);
    }

    public virtual async Task PrepareCustomerModel(CustomerModel model, Customer customer, bool excludeProperties)
    {
        var hasStaffStore = !string.IsNullOrEmpty(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId);
        var allStores = hasStaffStore ? [await _storeService.GetStoreById(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId)] : await _storeService.GetAllStores();
        if (customer != null)
        {
            model.Id = customer.Id;
            model.ShowMessageContactForm = _commonSettings.StoreInDatabaseContactUsForm;
            await PrepareSubAccounts(model, customer);
            await PrepareTradeCredit(model, customer);
            if (!excludeProperties)
            {
                model.Email = customer.Email;
                model.Username = customer.Username;
                model.VendorId = customer.VendorId;
                model.StaffStoreId = customer.StaffStoreId;
                model.StoreId = customer.StoreId;
                model.SeId = customer.SeId;
                model.AdminComment = customer.AdminComment;
                model.IsTaxExempt = customer.IsTaxExempt;
                model.FreeShipping = customer.FreeShipping;
                model.CreditLimit = customer.CreditLimit;
                model.PaymentTermDays = customer.PaymentTermDays;
                model.TwoFactorEnabled =
                    customer.GetUserFieldFromEntity<bool>(SystemCustomerFieldNames.TwoFactorEnabled);
                model.Active = customer.Active;
                model.Owner = await _groupService.IsOwner(customer)
                    ? ""
                    : (await _customerService.GetCustomerById(customer.OwnerId))?.Email;
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
                model.VatNumberStatusNote = _enumTranslationService.GetTranslationEnum((VatNumberStatus)customer.GetUserFieldFromEntity<int>(SystemCustomerFieldNames.VatNumberStatusId));
                model.CreatedOn = _dateTimeService.ConvertToUserTime(customer.CreatedOnUtc, DateTimeKind.Utc);
                model.LastActivityDate =
                    _dateTimeService.ConvertToUserTime(customer.LastActivityDateUtc, DateTimeKind.Utc);
                if (customer.LastPurchaseDateUtc.HasValue)
                    model.LastPurchaseDate =
                        _dateTimeService.ConvertToUserTime(customer.LastPurchaseDateUtc.Value, DateTimeKind.Utc);
                model.LastIpAddress = customer.LastIpAddress;
                model.LastVisitedPage = customer.LastVisitedPage;

                model.CustomerGroups = customer.Groups.ToArray();
                //newsletter subscriptions
                if (!string.IsNullOrEmpty(customer.Email))
                {
                    var newsletterSubscriptionStoreIds = new List<string>();
                    foreach (var store in allStores)
                    {
                        var newsletterSubscription = await _newsLetterSubscriptionService
                            .GetNewsLetterSubscriptionByEmailAndStoreId(customer.Email, store.Id);
                        if (newsletterSubscription is { Active: true })
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
                model.StateProvinceId =
                    customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StateProvinceId);
                model.Phone = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Phone);
                model.Fax = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Fax);
            }
        }
        else
        {
            model.SeId = _contextAccessor.WorkContext.CurrentCustomer.SeId;

            //a store manager can only create customers for his own store - preset it
            if (await _groupService.IsStoreManager(_contextAccessor.WorkContext.CurrentCustomer))
                model.StoreId = _contextAccessor.StoreContext.CurrentStore.Id;
        }

        model.UsernamesEnabled = _customerSettings.UsernamesEnabled;
        model.AllowUsersToChangeUsernames = _customerSettings.AllowUsersToChangeUsernames;
        model.DisplayVatNumber = customer != null && _taxSettings.EuVatEnabled;

        //stores
        await PrepareStoresModel(model);

        //employees
        await PrepareSalesEmployeeModel(model);

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
            model.AvailableCountries.Add(new SelectListItem
                { Text = _translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
            foreach (var c in await _countryService.GetAllCountries(showHidden: true))
                model.AvailableCountries.Add(new SelectListItem {
                    Text = c.Name,
                    Value = c.Id,
                    Selected = c.Id == model.CountryId
                });

            if (_customerSettings.StateProvinceEnabled)
            {
                //states
                var states = (await _countryService.GetCountryById(model.CountryId))?.StateProvinces;
                model.AvailableStates.Add(new SelectListItem
                    { Text = _translationService.GetResource("Admin.Address.SelectState"), Value = "" });
                if (states != null)
                    foreach (var s in states)
                        model.AvailableStates.Add(new SelectListItem
                            { Text = s.Name, Value = s.Id, Selected = s.Id == model.StateProvinceId });
            }
        }

        //newsletter subscriptions
        model.AvailableNewsletterSubscriptionStores = allStores
            .Select(s => new StoreModel { Id = s.Id, Name = s.Shortcut })
            .ToList();


        if (customer != null)
        {
            //loyalty points history
            model.DisplayLoyaltyPointsHistory = _loyaltyPointsSettings.Enabled;
            model.AddLoyaltyPointsValue = 0;
            model.AddLoyaltyPointsMessage = "Some comment here...";

            //stores
            foreach (var store in allStores)
                model.LoyaltyPointsAvailableStores.Add(new SelectListItem {
                    Text = store.Shortcut,
                    Value = store.Id,
                    Selected = store.Id == _contextAccessor.StoreContext.CurrentStore.Id
                });

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
        model.AllowSendingOfWelcomeMessage =
            _customerSettings.UserRegistrationType == UserRegistrationType.AdminApproval &&
            customer != null &&
            !string.IsNullOrEmpty(customer.Email);
        //sending of the activation message
        //1. "email validation" registration method
        //2. already created customer
        //3. has email
        //4. not active
        model.AllowReSendingOfActivationMessage =
            _customerSettings.UserRegistrationType == UserRegistrationType.EmailValidation &&
            customer != null &&
            !string.IsNullOrEmpty(customer.Email) &&
            !customer.Active;
    }

    /// <summary>
    ///     The owner id the posted owner email stands for. The owner already linked is kept when its email is
    ///     posted back unchanged; otherwise the email is looked up in the same store scope the validator used.
    /// </summary>
    private async Task<string> ResolveOwnerId(Customer customer, CustomerModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Owner))
            return string.Empty;

        var ownerEmail = model.Owner.Trim();
        if (!string.IsNullOrEmpty(customer?.OwnerId) &&
            (!_customerConfig.RegisterCustomersPerStore || customer.StoreId == model.StoreId))
        {
            var currentOwner = await _customerService.GetCustomerById(customer.OwnerId);
            if (currentOwner != null &&
                string.Equals(currentOwner.Email, ownerEmail, StringComparison.OrdinalIgnoreCase))
                return customer.OwnerId;
        }

        var storeId = _customerConfig.RegisterCustomersPerStore ? model.StoreId : "";
        var owner = await _customerService.GetCustomerByEmail(ownerEmail, storeId);
        return owner?.Id ?? customer?.OwnerId ?? string.Empty;
    }

    //what a company owner owes on orders bought on credit; a sub-account buys on its owner's credit
    private async Task PrepareTradeCredit(CustomerModel model, Customer customer)
    {
        if (!string.IsNullOrEmpty(customer.OwnerId))
            return;

        var outstanding = await _companyCreditService.GetOutstandingAmount(customer.Id);
        model.OutstandingAmount =
            _priceFormatter.FormatPrice(outstanding, await _currencyService.GetPrimaryStoreCurrency());
        model.HasOverdue = await _companyCreditService.HasOverdue(customer.Id, DateTime.UtcNow);
    }

    private async Task PrepareSubAccounts(CustomerModel model, Customer customer)
    {
        model.OwnerCustomerId = string.Empty;
        model.OwnerCustomerEmail = string.Empty;
        model.SubAccounts.Clear();
        //sub-accounts are one level deep, so a sub-account never has its own - only an owner, linked when the
        //panel may open it
        if (!string.IsNullOrEmpty(customer.OwnerId))
        {
            var owner = await _customerService.GetCustomerById(customer.OwnerId);
            if (owner != null && await _scope.HasAccess(owner))
            {
                model.OwnerCustomerId = owner.Id;
                model.OwnerCustomerEmail = owner.Email;
            }

            return;
        }

        //scoped in the query like the customer list (store of the Store panel, sales employee of a sales
        //manager); the panel's own access check then guards each row the same way it guards opening it
        var storeId = await _scope.GetDefaultStoreIdAsync() ?? "";
        var subAccounts = await _customerService.GetAllCustomers(ownerId: customer.Id, storeId: storeId,
            salesEmployeeId: _contextAccessor.WorkContext.CurrentCustomer.SeId);
        //the limit is kept in the primary store currency; loaded once, and only when some row has a limit
        var currency = subAccounts.Any(x => x.OrderApprovalLimit.HasValue)
            ? await _currencyService.GetPrimaryStoreCurrency()
            : null;
        foreach (var item in subAccounts)
        {
            if (!await _scope.HasAccess(item))
                continue;

            model.SubAccounts.Add(new CustomerModel.SubAccountModel {
                Id = item.Id,
                Email = item.Email,
                FullName = item.GetFullName(),
                Active = item.Active,
                Role = _enumTranslationService.GetTranslationEnum(item.SubAccountRole),
                OrderApprovalLimit = item.OrderApprovalLimit.HasValue
                    ? _priceFormatter.FormatPrice(item.OrderApprovalLimit.Value, currency)
                    : string.Empty
            });
        }
    }

    public virtual async Task<Customer> InsertCustomerModel(CustomerModel model)
    {
        var ownerId = await ResolveOwnerId(null, model);

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
            StoreId = model.StoreId,
            OwnerId = ownerId,
            CreditLimit = string.IsNullOrEmpty(ownerId) ? model.CreditLimit : null,
            PaymentTermDays = string.IsNullOrEmpty(ownerId) ? model.PaymentTermDays : null,
            Attributes = model.Attributes,
            LastActivityDateUtc = DateTime.UtcNow
        };

        //user fields
        customer.UserFields.Add(new UserField
            { Key = SystemCustomerFieldNames.FirstName, Value = model.FirstName, StoreId = "" });
        customer.UserFields.Add(new UserField
            { Key = SystemCustomerFieldNames.LastName, Value = model.LastName, StoreId = "" });

        if (_customerSettings.GenderEnabled)
            customer.UserFields.Add(new UserField
                { Key = SystemCustomerFieldNames.Gender, Value = model.Gender, StoreId = "" });

        if (_customerSettings.DateOfBirthEnabled && model.DateOfBirth.HasValue)
            customer.UserFields.Add(new UserField
                { Key = SystemCustomerFieldNames.DateOfBirth, Value = model.DateOfBirth.ToString(), StoreId = "" });
        if (_customerSettings.CompanyEnabled)
            customer.UserFields.Add(new UserField
                { Key = SystemCustomerFieldNames.Company, Value = model.Company, StoreId = "" });
        if (_customerSettings.StreetAddressEnabled)
            customer.UserFields.Add(new UserField
                { Key = SystemCustomerFieldNames.StreetAddress, Value = model.StreetAddress, StoreId = "" });
        if (_customerSettings.StreetAddress2Enabled)
            customer.UserFields.Add(new UserField
                { Key = SystemCustomerFieldNames.StreetAddress2, Value = model.StreetAddress2, StoreId = "" });
        if (_customerSettings.ZipPostalCodeEnabled)
            customer.UserFields.Add(new UserField
                { Key = SystemCustomerFieldNames.ZipPostalCode, Value = model.ZipPostalCode, StoreId = "" });
        if (_customerSettings.CityEnabled)
            customer.UserFields.Add(new UserField
                { Key = SystemCustomerFieldNames.City, Value = model.City, StoreId = "" });
        if (_customerSettings.CountryEnabled)
            customer.UserFields.Add(new UserField
                { Key = SystemCustomerFieldNames.CountryId, Value = model.CountryId, StoreId = "" });
        if (_customerSettings.CountryEnabled && _customerSettings.StateProvinceEnabled)
            customer.UserFields.Add(new UserField
                { Key = SystemCustomerFieldNames.StateProvinceId, Value = model.StateProvinceId, StoreId = "" });
        if (_customerSettings.PhoneEnabled)
            customer.UserFields.Add(new UserField
                { Key = SystemCustomerFieldNames.Phone, Value = model.Phone, StoreId = "" });
        if (_customerSettings.FaxEnabled)
            customer.UserFields.Add(new UserField
                { Key = SystemCustomerFieldNames.Fax, Value = model.Fax, StoreId = "" });

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
                        await _newsLetterSubscriptionService.InsertNewsLetterSubscription(new NewsLetterSubscription {
                            NewsLetterSubscriptionGuid = Guid.NewGuid(),
                            CustomerId = customer.Id,
                            Email = customer.Email,
                            Active = true,
                            StoreId = store.Id
                        });
                }
                else
                {
                    //not subscribed
                    if (newsletterSubscription != null)
                        await _newsLetterSubscriptionService.DeleteNewsLetterSubscription(newsletterSubscription);
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

        //tags
        await SaveCustomerTags(customer, ParseCustomerTags(model.CustomerTags));

        return customer;
    }

    public virtual async Task<Customer> UpdateCustomerModel(Customer customer, CustomerModel model)
    {
        customer.AdminComment = model.AdminComment;
        customer.IsTaxExempt = model.IsTaxExempt;
        customer.FreeShipping = model.FreeShipping;
        customer.Active = model.Active;
        customer.Attributes = model.Attributes;

        if (!model.TwoFactorEnabled)
            await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.TwoFactorEnabled,
                model.TwoFactorEnabled);

        customer.Email = model.Email;

        customer.Username = _customerSettings.UsernamesEnabled switch {
            true when _customerSettings.AllowUsersToChangeUsernames => model.Username?.Trim(),
            false => model.Email?.Trim(),
            _ => customer.Username
        };

        customer.OwnerId = await ResolveOwnerId(customer, model);
        //only a company owner has trade credit of its own
        customer.CreditLimit = string.IsNullOrEmpty(customer.OwnerId) ? model.CreditLimit : null;
        customer.PaymentTermDays = string.IsNullOrEmpty(customer.OwnerId) ? model.PaymentTermDays : null;

        //VAT number
        if (_taxSettings.EuVatEnabled)
        {
            var prevVatNumber = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.VatNumber);

            await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.VatNumber, model.VatNumber);
            //set VAT number status
            if (!string.IsNullOrEmpty(model.VatNumber))
            {
                if (!model.VatNumber.Equals(prevVatNumber, StringComparison.OrdinalIgnoreCase))
                {
                    var checkVatService =
                        _httpContextAccessor.HttpContext!.RequestServices.GetRequiredService<IVatService>();
                    await _customerService.UpdateUserField(customer,
                        SystemCustomerFieldNames.VatNumberStatusId,
                        (int)(await checkVatService.GetVatNumberStatus(model.VatNumber)).status);
                }
            }
            else
            {
                await _customerService.UpdateUserField(customer,
                    SystemCustomerFieldNames.VatNumberStatusId,
                    (int)VatNumberStatus.Empty);
            }
        }

        //vendor
        customer.VendorId = model.VendorId;

        //staff store
        customer.StaffStoreId = model.StaffStoreId;

        //store
        customer.StoreId = model.StoreId;

        //sales employee
        customer.SeId = model.SeId;

        //user fields
        if (_customerSettings.GenderEnabled)
            await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.Gender, model.Gender);
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.FirstName, model.FirstName);
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.LastName, model.LastName);
        if (_customerSettings.DateOfBirthEnabled)
            await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.DateOfBirth, model.DateOfBirth);
        if (_customerSettings.CompanyEnabled)
            await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.Company, model.Company);
        if (_customerSettings.StreetAddressEnabled)
            await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.StreetAddress, model.StreetAddress);
        if (_customerSettings.StreetAddress2Enabled)
            await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.StreetAddress2, model.StreetAddress2);
        if (_customerSettings.ZipPostalCodeEnabled)
            await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.ZipPostalCode, model.ZipPostalCode);
        if (_customerSettings.CityEnabled)
            await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.City, model.City);
        if (_customerSettings.CountryEnabled)
            await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.CountryId, model.CountryId);
        if (_customerSettings.CountryEnabled && _customerSettings.StateProvinceEnabled)
            await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.StateProvinceId,
                model.StateProvinceId);
        if (_customerSettings.PhoneEnabled)
            await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.Phone, model.Phone);
        if (_customerSettings.FaxEnabled)
            await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.Fax, model.Fax);

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
                        await _newsLetterSubscriptionService.InsertNewsLetterSubscription(new NewsLetterSubscription {
                            NewsLetterSubscriptionGuid = Guid.NewGuid(),
                            CustomerId = customer.Id,
                            Email = customer.Email,
                            Active = true,
                            StoreId = store.Id
                        });
                }
                else
                {
                    //not subscribed
                    if (newsletterSubscription != null)
                        await _newsLetterSubscriptionService.DeleteNewsLetterSubscription(newsletterSubscription);
                }
            }
        }

        var allCustomerGroups = await _groupService.GetAllCustomerGroups(showHidden: true);

        //customer groups
        foreach (var customerGroup in allCustomerGroups)
            if (model.CustomerGroups != null &&
                model.CustomerGroups.Contains(customerGroup.Id))
            {
                //new role
                if (customer.Groups.Count(cr => cr == customerGroup.Id) == 0) customer.Groups.Add(customerGroup.Id);
            }
            else
            {
                //remove role
                if (customer.Groups.Count(cr => cr == customerGroup.Id) > 0)
                    customer.Groups.Remove(customer.Groups.First(x => x == customerGroup.Id));
            }

        await _customerService.UpdateCustomerInAdminPanel(customer);

        //tags
        await SaveCustomerTags(customer, ParseCustomerTags(model.CustomerTags));

        return customer;
    }

    public virtual async Task DeleteCustomer(Customer customer)
    {
        await _customerService.DeleteCustomer(customer);

        //remove newsletter subscription (if exists)
        foreach (var store in await _storeService.GetAllStores())
        {
            var subscription =
                await _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreId(customer.Email,
                    store.Id);
            if (subscription != null)
                await _newsLetterSubscriptionService.DeleteNewsLetterSubscription(subscription);
        }
    }

    public virtual async Task DeleteSelected(IEnumerable<string> selectedIds)
    {
        var customers = new List<Customer>();
        customers.AddRange(await _customerService.GetCustomersByIds(selectedIds.ToArray()));
        foreach (var customer in customers.Where(customer => customer.Id != _contextAccessor.WorkContext.CurrentCustomer.Id))
            await _customerService.DeleteCustomer(customer);
    }

    public async Task SendEmail(Customer customer, CustomerModel.SendEmailModel model)
    {
        var emailAccountService =
            _httpContextAccessor.HttpContext!.RequestServices.GetRequiredService<IEmailAccountService>();
        var emailAccountSettings =
            _httpContextAccessor.HttpContext!.RequestServices.GetRequiredService<EmailAccountSettings>();
        var queuedEmailService =
            _httpContextAccessor.HttpContext!.RequestServices.GetRequiredService<IQueuedEmailService>();

        var emailAccount = await emailAccountService.GetEmailAccountById(emailAccountSettings.DefaultEmailAccountId) ??
                           (await emailAccountService.GetAllEmailAccounts()).FirstOrDefault();
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
            DontSendBeforeDateUtc = model.SendImmediately || !model.DontSendBeforeDate.HasValue
                ? null
                : _dateTimeService.ConvertToUtcTime(model.DontSendBeforeDate.Value)
        };
        await queuedEmailService.InsertQueuedEmail(email);
    }

    #region Utilities

    protected virtual string[] ParseCustomerTags(string customerTags)
    {
        var result = new List<string>();
        if (!string.IsNullOrWhiteSpace(customerTags))
        {
            var values = customerTags.Split([','], StringSplitOptions.RemoveEmptyEntries);
            foreach (var val1 in values)
                if (!string.IsNullOrEmpty(val1.Trim()))
                    result.Add(val1.Trim());
        }

        return result.ToArray();
    }

    protected virtual async Task SaveCustomerTags(Customer customer, string[] customerTags)
    {
        ArgumentNullException.ThrowIfNull(customer);

        //product tags
        var existingCustomerTags = customer.CustomerTags.ToList();
        var customerTagsToRemove = new List<CustomerTag>();
        foreach (var existingCustomerTag in existingCustomerTags)
        {
            var found = false;
            var existingCustomerTagName = await _customerTagService.GetCustomerTagById(existingCustomerTag);
            foreach (var newCustomerTag in customerTags)
                if (existingCustomerTagName.Name.Equals(newCustomerTag, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }

            if (!found)
            {
                customerTagsToRemove.Add(existingCustomerTagName);
                await _customerTagService.DeleteTagFromCustomer(existingCustomerTagName.Id, customer.Id);
            }
        }

        foreach (var customerTagName in customerTags)
        {
            CustomerTag customerTag;
            var customerTag2 = await _customerTagService.GetCustomerTagByName(customerTagName);
            if (customerTag2 == null)
            {
                customerTag = new CustomerTag {
                    Name = customerTagName
                };
                await _customerTagService.InsertCustomerTag(customerTag);
            }
            else
            {
                customerTag = customerTag2;
            }

            if (!customer.CustomerTags.Contains(customerTag.Id))
                await _customerTagService.InsertTagToCustomer(customerTag.Id, customer.Id);
        }
    }

    protected virtual string GetCustomerGroupsNames(IList<CustomerGroup> customerGroups, string separator = ",")
    {
        var sb = new StringBuilder();
        for (var i = 0; i < customerGroups.Count; i++)
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

    protected virtual async Task<IList<CustomerModel.AssociatedExternalAuthModel>> GetAssociatedExternalAuthRecords(
        Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        var openAuthenticationService = _httpContextAccessor.HttpContext!.RequestServices
            .GetRequiredService<IExternalAuthenticationService>();
        var result = new List<CustomerModel.AssociatedExternalAuthModel>();
        foreach (var record in await openAuthenticationService.GetExternalIdentifiers(customer))
        {
            var method = openAuthenticationService.LoadAuthenticationProviderBySystemName(record.ProviderSystemName);
            //the resource server makes OpenID Connect links without the sign-in plugin: they are listed (and can be
            //removed) all the same
            var isOidcLink = record.ProviderSystemName == OidcResourceServerDefaults.ProviderSystemName;
            if (method == null && !isOidcLink)
                continue;

            result.Add(new CustomerModel.AssociatedExternalAuthModel {
                Id = record.Id,
                Email = record.Email,
                ExternalIdentifier = record.ExternalIdentifier,
                AuthMethodName = method?.FriendlyName ?? "OpenID Connect"
            });
        }

        return result;
    }

    protected virtual async Task<CustomerModel> PrepareCustomerModelForList(Customer customer)
    {
        return new CustomerModel {
            Id = customer.Id,
            Email = !string.IsNullOrEmpty(customer.Email)
                ? customer.Email
                : _translationService.GetResource("Admin.Customers.Guest"),
            Username = customer.Username,
            FullName = customer.GetFullName(),
            Company = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Company),
            Phone = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Phone),
            ZipPostalCode = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.ZipPostalCode),
            CustomerGroupNames = GetCustomerGroupsNames(await _groupService.GetAllByIds(customer.Groups.ToArray())),
            Active = customer.Active,
            CreatedOn = _dateTimeService.ConvertToUserTime(customer.CreatedOnUtc, DateTimeKind.Utc),
            LastActivityDate = _dateTimeService.ConvertToUserTime(customer.LastActivityDateUtc, DateTimeKind.Utc)
        };
    }

    protected virtual async Task PrepareSalesEmployeeModel(CustomerModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        model.AvailableSalesEmployees.Add(new SelectListItem {
            Text = _translationService.GetResource("Admin.Customers.Customers.Fields.SalesEmployee.None"),
            Value = ""
        });
        var employees = await _salesEmployeeService.GetAll();
        foreach (var employee in employees.Where(x => x.Active))
            model.AvailableSalesEmployees.Add(new SelectListItem {
                Text = employee.Name,
                Value = employee.Id
            });
    }


    protected virtual async Task PrepareStoresModel(CustomerModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        model.AvailableStores.Add(new SelectListItem {
            Text = _translationService.GetResource("Admin.Customers.Customers.Fields.StaffStore.None"),
            Value = ""
        });
        var stores = await _storeService.GetAllStores();
        foreach (var store in stores)
            model.AvailableStores.Add(new SelectListItem {
                Text = store.Shortcut,
                Value = store.Id
            });
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
                AttributeControlType = attribute.AttributeControlTypeId
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
                            var selectedValues =
                                await _customerAttributeParser.ParseCustomerAttributeValues(customer.Attributes);
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
                            var enteredText = customer.Attributes.Where(x => x.Key == attribute.Id).Select(x => x.Value)
                                .ToList();
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

            model.CustomerAttributes.Add(attributeModel);
        }
    }

    #endregion
}