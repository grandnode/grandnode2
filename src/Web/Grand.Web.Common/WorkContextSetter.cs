using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Wangkanai.Detection.Services;

namespace Grand.Web.Common;

/// <summary>
///     Represents work context for web application / current customer / store / language / currency
/// </summary>
public class WorkContextSetter : IWorkContextSetter
{
    #region Fields

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IGrandAuthenticationService _authenticationService;
    private readonly ICurrencyService _currencyService;
    private readonly ICustomerService _customerService;
    private readonly IGroupService _groupService;
    private readonly ILanguageService _languageService;
    private readonly IStoreService _storeService;
    private readonly IAclService _aclService;
    private readonly IVendorService _vendorService;

    private readonly TaxSettings _taxSettings;
    private readonly AppConfig _config;

    private Customer _originalCustomerIfImpersonated;

    #endregion
    #region Ctor

    public WorkContextSetter(
        IHttpContextAccessor httpContextAccessor,
        IGrandAuthenticationService authenticationService,
        ICurrencyService currencyService,
        ICustomerService customerService,
        IGroupService groupService,
        ILanguageService languageService,
        IStoreService storeService,
        IAclService aclService,
        IVendorService vendorService,
        TaxSettings taxSettings,
        AppConfig config)
    {
        _httpContextAccessor = httpContextAccessor;
        _authenticationService = authenticationService;
        _currencyService = currencyService;
        _customerService = customerService;
        _groupService = groupService;
        _languageService = languageService;
        _storeService = storeService;
        _aclService = aclService;
        _vendorService = vendorService;
        _taxSettings = taxSettings;
        _config = config;
    }

    #endregion

    #region Utilities

    /// <summary>
    ///     Get language from the requested page URL
    /// </summary>
    /// <returns>The found language</returns>
    protected virtual async Task<Language> GetLanguageFromUrl(IList<Language> languages, Store store)
    {
        if (_httpContextAccessor.HttpContext?.Request == null)
            return await Task.FromResult<Language>(null);

        var path = _httpContextAccessor.HttpContext.Request.Path.Value;
        if (string.IsNullOrEmpty(path))
            return await Task.FromResult<Language>(null);

        var firstSegment = path.Split(['/'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

        if (string.IsNullOrEmpty(firstSegment))
            return await Task.FromResult<Language>(null);

        var language = languages.FirstOrDefault(urlLanguage =>
            urlLanguage.UniqueSeoCode.Equals(firstSegment, StringComparison.OrdinalIgnoreCase));

        if (language is not { Published: true } || !_aclService.Authorize(language, store.Id))
            return await Task.FromResult<Language>(null);

        return language;
    }

    /// <summary>
    ///     Get language from the request
    /// </summary>
    /// <returns>The found language</returns>
    protected virtual async Task<Language> GetLanguageFromRequest(IList<Language> languages, Store store)
    {
        if (_httpContextAccessor.HttpContext?.Request == null)
            return await Task.FromResult<Language>(null);

        //get request culture
        var requestCulture =
            _httpContextAccessor.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture;
        if (requestCulture == null)
            return await Task.FromResult<Language>(null);

        //try to get language by culture name
        var requestLanguage = languages.FirstOrDefault(language =>
            language.LanguageCulture.Equals(requestCulture.Culture.Name, StringComparison.OrdinalIgnoreCase));

        //check language availability
        if (requestLanguage is not { Published: true } ||
            !_aclService.Authorize(requestLanguage, store.Id))
            return await Task.FromResult<Language>(null);

        return requestLanguage;
    }

    protected virtual async Task<Customer> OriginalCustomerIfImpersonated(Customer customer)
    {
        var impersonatedCustomerId = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.ImpersonatedCustomerId);
        if (string.IsNullOrEmpty(impersonatedCustomerId)) return null;
        var impersonatedCustomer = await _customerService.GetCustomerById(impersonatedCustomerId);
        if (impersonatedCustomer is not { Deleted: false, Active: true }) return null;
        return impersonatedCustomer;
    }


    #endregion

    #region Properties

    public virtual async Task<IWorkContext> InitializeWorkContext(string storeId = null)
    {
        var currentStore = await CurrentStore(storeId);
        var workContext = new CurrentWorkContext {            
            CurrentCustomer = await CurrentCustomer(currentStore)
        };
        if (workContext.CurrentCustomer != null)
        {
            workContext.CurrentVendor = await CurrentVendor(workContext.CurrentCustomer);
            workContext.OriginalCustomerIfImpersonated = _originalCustomerIfImpersonated;
            workContext.WorkingLanguage = await WorkingLanguage(workContext.CurrentCustomer, currentStore);
            workContext.WorkingCurrency = await WorkingCurrency(workContext.CurrentCustomer, workContext.WorkingLanguage, currentStore);
            workContext.TaxDisplayType = await TaxDisplayType(workContext.CurrentCustomer, currentStore);
        }
        return workContext;
    }

    /// <summary>
    ///     Set the current customer by Middleware
    /// </summary>
    /// <returns></returns>
    protected async Task<Customer> CurrentCustomer(Store store)
    {
        var customer = await GetBackgroundTaskCustomer();
        if (customer != null) return customer;

        customer = await GetAllowAnonymousCustomer();
        if (customer != null) return customer;

        customer = await GetCookieAuthenticatedCustomer();
        if (customer != null) return customer;

        customer = await GetGuestCustomer();
        if (customer != null) return customer;

        customer = await GetSearchEngineCustomer();
        if (customer != null) return customer;

        customer = await GetApiUserCustomer();
        if (customer != null) return customer;

        //create guest if not exists
        customer = await CreateCustomerGuest(store);

        return customer;
    }

    private async Task<Customer> GetBackgroundTaskCustomer()
    {
        if (_httpContextAccessor.HttpContext != null) return null;

        return await _customerService.GetCustomerBySystemName(SystemCustomerNames.BackgroundTask);
    }

    private async Task<Customer> GetAllowAnonymousCustomer()
    {
        var endpoint = _httpContextAccessor.HttpContext?.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() == null) return null;

        return await _customerService.GetCustomerBySystemName(SystemCustomerNames.Anonymous);
    }

    private async Task<Customer> GetCookieAuthenticatedCustomer()
    {
        var customer = await _authenticationService.GetAuthenticatedCustomer();
        if (customer == null) return null;

        var impersonatedCustomer = await ImpersonatedCustomer(customer);
        if (impersonatedCustomer != null)
        {
            _originalCustomerIfImpersonated = customer;
            return impersonatedCustomer;
        }
        return customer;
    }

    private async Task<Customer> GetApiUserCustomer()
    {
        var apiAuthenticationService = _httpContextAccessor.HttpContext.RequestServices.GetService<IApiAuthenticationService>();
        return await apiAuthenticationService.GetAuthenticatedCustomer();
    }

    private async Task<Customer> GetSearchEngineCustomer()
    {
        var detectionService = _httpContextAccessor.HttpContext.RequestServices.GetService<IDetectionService>();
        var isCrawler = detectionService.Crawler?.IsCrawler;
        if (!isCrawler.GetValueOrDefault()) return null;

        return await _customerService.GetCustomerBySystemName(SystemCustomerNames.SearchEngine);
    }

    private async Task<Customer> GetGuestCustomer()
    {
        var guid = await _authenticationService.GetCustomerGuid();
        if (string.IsNullOrEmpty(guid) || !Guid.TryParse(guid, out var customerGuid)) return null;

        var customerByGuid = await _customerService.GetCustomerByGuid(customerGuid);
        if (customerByGuid is { Deleted: false, Active: true } && !await _groupService.IsRegistered(customerByGuid))
            return customerByGuid;

        return null;
    }

    private async Task<Customer> CreateCustomerGuest(Store store)
    {
        var userFields = new List<UserField>();

        if (!string.IsNullOrEmpty(store.DefaultCurrencyId))
            userFields.Add(new UserField {
                Key = SystemCustomerFieldNames.CurrencyId,
                Value = store.DefaultCurrencyId,
                StoreId = store.Id
            });

        if (!string.IsNullOrEmpty(store.DefaultLanguageId))
            userFields.Add(new UserField {
                Key = SystemCustomerFieldNames.LanguageId,
                Value = store.DefaultLanguageId,
                StoreId = store.Id
            });

        var customer = new Customer {
            CustomerGuid = Guid.NewGuid(),
            Active = true,
            StoreId = store.Id,
            LastActivityDateUtc = DateTime.UtcNow,
            LastIpAddress = _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
            UserFields = userFields
        };

        customer = await _customerService.InsertGuestCustomer(customer);

        //set customer cookie
        await _authenticationService.SetCustomerGuid(customer.CustomerGuid);
        return customer;
    }

    /// <summary>
    /// Get Impersonated customer
    /// </summary>
    protected async Task<Customer> ImpersonatedCustomer(Customer customer)
    {
        var impersonatedCustomerId = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.ImpersonatedCustomerId);
        if (string.IsNullOrEmpty(impersonatedCustomerId)) return null;
        var impersonatedCustomer = await _customerService.GetCustomerById(impersonatedCustomerId);
        if (impersonatedCustomer is not { Deleted: false, Active: true }) return null;
        return impersonatedCustomer;
    }


    /// <summary>
    ///     Set the current vendor (logged-in manager)
    /// </summary>
    protected async Task<Vendor> CurrentVendor(Customer customer)
    {
        if (customer == null)
            return await Task.FromResult<Vendor>(null);

        if (string.IsNullOrEmpty(customer.VendorId))
            return await Task.FromResult<Vendor>(null);

        //try to get vendor
        var vendor = await _vendorService.GetVendorById(customer.VendorId);

        //check vendor availability
        if (vendor == null || vendor.Deleted || !vendor.Active)
            return await Task.FromResult<Vendor>(null);

        //cache the found vendor
        return vendor;
    }

    /// <summary>
    ///     Set current user working language by Middleware
    /// </summary>
    /// <param name="customer"></param>
    /// <returns></returns>
    protected async Task<Language> WorkingLanguage(Customer customer, Store store)
    {
        Language language = null;

        var adminAreaUrl =
            _httpContextAccessor.HttpContext?.Request.Path.StartsWithSegments(new PathString("/Admin"));
        var allStoreLanguages = await _languageService.GetAllLanguages(adminAreaUrl ?? false);

        if (allStoreLanguages.Count == 1)
            return allStoreLanguages.FirstOrDefault();

        if (_config.SeoFriendlyUrlsForLanguagesEnabled)
            language = await GetLanguageFromUrl(allStoreLanguages, store);

        //get current lang identifier
        var customerLanguageId =
            customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LanguageId, store.Id);

        if (language == null && string.IsNullOrEmpty(customerLanguageId))
            language = await GetLanguageFromRequest(allStoreLanguages, store);

        //check customer language 
        var customerLanguage = (language ??
                                (!string.IsNullOrEmpty(customerLanguageId)
                                    ? allStoreLanguages.FirstOrDefault(lang => lang.Id == customerLanguageId)
                                    : allStoreLanguages.FirstOrDefault(lang =>
                                        //if the language for the current store not exist, then use the first
                                        lang.Id == store.DefaultLanguageId))) ??
                               allStoreLanguages.FirstOrDefault();

        return customerLanguage ?? throw new Exception("No language could be loaded");
    }


    /// <summary>
    ///     Set current user working currency by Middleware
    /// </summary>
    protected async Task<Currency> WorkingCurrency(Customer customer, Language language, Store store)
    {
        //return store currency when we're you are in admin panel
        var adminAreaUrl =
            _httpContextAccessor.HttpContext?.Request.Path.StartsWithSegments(new PathString("/Admin"));
        if (adminAreaUrl.HasValue && adminAreaUrl.Value)
        {
            var primaryStoreCurrency = await _currencyService.GetPrimaryStoreCurrency();
            if (primaryStoreCurrency != null)
            {
                return primaryStoreCurrency;
            }
        }

        var allStoreCurrencies = await _currencyService.GetAllCurrencies(storeId: store.Id);

        if (allStoreCurrencies.Count == 1)
            return allStoreCurrencies.FirstOrDefault();

        var customerCurrencyId =
            customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.CurrencyId, store.Id);

        var customerCurrency = (allStoreCurrencies.FirstOrDefault(currency => currency.Id == customerCurrencyId) ??
                                (!string.IsNullOrEmpty(store.DefaultCurrencyId)
                                    ? allStoreCurrencies.FirstOrDefault(currency =>
                                        currency.Id == store.DefaultCurrencyId)
                                    : allStoreCurrencies.FirstOrDefault(currency =>
                                        currency.Id == language.DefaultCurrencyId))) ??
                               allStoreCurrencies.FirstOrDefault();


        return customerCurrency ?? throw new Exception("No currency could be loaded");
    }

    protected async Task<TaxDisplayType> TaxDisplayType(Customer customer, Store store)
    {
        TaxDisplayType taxDisplayType;

        if (_taxSettings.AllowCustomersToSelectTaxDisplayType && customer != null)
        {
            var taxDisplayTypeId =
                customer.GetUserFieldFromEntity<int>(SystemCustomerFieldNames.TaxDisplayTypeId, store.Id);
            taxDisplayType = (TaxDisplayType)taxDisplayTypeId;
        }
        else
        {
            //or get the default tax display type
            taxDisplayType = _taxSettings.TaxDisplayType;
        }

        return await Task.FromResult(taxDisplayType);
    }


    protected async Task<Store> CurrentStore(string id = null)
    {
        if (!string.IsNullOrEmpty(id))
            return await _storeService.GetStoreById(id);

        return (await _storeService.GetAllStores()).FirstOrDefault();
    }

    private sealed class CurrentWorkContext : IWorkContext
    {
        public Customer CurrentCustomer { get; set; }

        public Customer OriginalCustomerIfImpersonated { get; set; }

        public Vendor CurrentVendor { get; set; }

        public Language WorkingLanguage { get; set; }

        public Currency WorkingCurrency { get; set; }

        public TaxDisplayType TaxDisplayType { get; set; }
    }

    #endregion
}