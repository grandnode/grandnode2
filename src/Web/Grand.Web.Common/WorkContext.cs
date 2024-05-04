using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
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
public class WorkContext : IWorkContext, IWorkContextSetter
{
    #region Ctor

    public WorkContext(
        IHttpContextAccessor httpContextAccessor,
        IGrandAuthenticationService authenticationService,
        ICurrencyService currencyService,
        ICustomerService customerService,
        IGroupService groupService,
        ILanguageService languageService,
        IStoreHelper storeHelper,
        IAclService aclService,
        IVendorService vendorService,
        LanguageSettings languageSettings,
        TaxSettings taxSettings,
        AppConfig config)
    {
        _httpContextAccessor = httpContextAccessor;
        _authenticationService = authenticationService;
        _currencyService = currencyService;
        _customerService = customerService;
        _groupService = groupService;
        _languageService = languageService;
        _storeHelper = storeHelper;
        _aclService = aclService;
        _vendorService = vendorService;
        _languageSettings = languageSettings;
        _taxSettings = taxSettings;
        _config = config;
    }

    #endregion

    #region Fields

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IGrandAuthenticationService _authenticationService;
    private readonly ICurrencyService _currencyService;
    private readonly ICustomerService _customerService;
    private readonly IGroupService _groupService;
    private readonly ILanguageService _languageService;
    private readonly IStoreHelper _storeHelper;
    private readonly IAclService _aclService;
    private readonly IVendorService _vendorService;

    private readonly LanguageSettings _languageSettings;
    private readonly TaxSettings _taxSettings;
    private readonly AppConfig _config;

    private Customer _cachedCustomer;
    private Customer _originalCustomerIfImpersonated;
    private Vendor _cachedVendor;
    private Language _cachedLanguage;
    private Currency _cachedCurrency;

    private TaxDisplayType _cachedTaxDisplayType;

    #endregion

    #region Utilities

    /// <summary>
    ///     Get language from the requested page URL
    /// </summary>
    /// <returns>The found language</returns>
    protected virtual async Task<Language> GetLanguageFromUrl(IList<Language> languages)
    {
        if (_httpContextAccessor.HttpContext?.Request == null)
            return await Task.FromResult<Language>(null);

        var path = _httpContextAccessor.HttpContext.Request.Path.Value;
        if (string.IsNullOrEmpty(path))
            return await Task.FromResult<Language>(null);

        var firstSegment = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ??
                           string.Empty;
        if (string.IsNullOrEmpty(firstSegment))
            return await Task.FromResult<Language>(null);

        var language = languages.FirstOrDefault(urlLanguage =>
            urlLanguage.UniqueSeoCode.Equals(firstSegment, StringComparison.OrdinalIgnoreCase));

        if (language is not { Published: true } || !_aclService.Authorize(language, CurrentStore.Id))
            return await Task.FromResult<Language>(null);

        return language;
    }

    /// <summary>
    ///     Get language from the request
    /// </summary>
    /// <returns>The found language</returns>
    protected virtual async Task<Language> GetLanguageFromRequest(IList<Language> languages)
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
            !_aclService.Authorize(requestLanguage, CurrentStore.Id))
            return await Task.FromResult<Language>(null);

        return requestLanguage;
    }

    protected virtual async Task<Customer> SetImpersonatedCustomer(Customer customer)
    {
        //get impersonate user if required
        var impersonatedCustomerId =
            customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.ImpersonatedCustomerId);
        if (string.IsNullOrEmpty(impersonatedCustomerId)) return null;
        var impersonatedCustomer = await _customerService.GetCustomerById(impersonatedCustomerId);
        if (impersonatedCustomer is not { Deleted: false, Active: true }) return null;
        //set impersonated customer
        _originalCustomerIfImpersonated = customer;
        return impersonatedCustomer;
    }

    #endregion

    #region Properties

    /// <summary>
    ///     Gets the current customer
    /// </summary>
    public virtual Customer CurrentCustomer => _cachedCustomer;

    /// <summary>
    ///     Set the current customer by Middleware
    /// </summary>
    /// <returns></returns>
    public virtual async Task<Customer> SetCurrentCustomer()
    {
        var customer = await GetBackgroundTaskCustomer();
        if (customer != null) return _cachedCustomer = customer;

        customer = await GetAuthenticatedCustomer();
        if (customer != null) return _cachedCustomer = customer;

        customer = await GetGuestCustomer();
        if (customer != null) return _cachedCustomer = customer;

        customer = await GetApiUserCustomer();
        if (customer != null) return _cachedCustomer = customer;

        customer = await GetSearchEngineCustomer();
        if (customer != null) return _cachedCustomer = customer;

        customer = await GetAllowAnonymousCustomer();
        if (customer != null) return _cachedCustomer = customer;

        //create guest if not exists
        customer = await CreateCustomerGuest();

        //cache the found customer
        return _cachedCustomer = customer;
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

    private async Task<Customer> GetAuthenticatedCustomer()
    {
        var customer = await _authenticationService.GetAuthenticatedCustomer();
        if (customer == null) return null;

        var impersonatedCustomer = await SetImpersonatedCustomer(customer);
        if (impersonatedCustomer != null)
            customer = impersonatedCustomer;

        return customer;
    }

    private async Task<Customer> GetApiUserCustomer()
    {
        var apiAuthenticationService =
            _httpContextAccessor.HttpContext.RequestServices.GetService<IApiAuthenticationService>();
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

    private async Task<Customer> CreateCustomerGuest()
    {
        var userFields = new List<UserField>();

        if (_httpContextAccessor?.HttpContext?.Request.GetTypedHeaders().Referer?.ToString() is { } referer)
            userFields.Add(new UserField { Key = SystemCustomerFieldNames.UrlReferrer, Value = referer, StoreId = "" });

        if (!string.IsNullOrEmpty(CurrentStore.DefaultCurrencyId))
            userFields.Add(new UserField {
                Key = SystemCustomerFieldNames.CurrencyId, Value = CurrentStore.DefaultCurrencyId,
                StoreId = CurrentStore.Id
            });

        if (!string.IsNullOrEmpty(CurrentStore.DefaultLanguageId))
            userFields.Add(new UserField {
                Key = SystemCustomerFieldNames.LanguageId, Value = CurrentStore.DefaultLanguageId,
                StoreId = CurrentStore.Id
            });

        var customer = new Customer {
            CustomerGuid = Guid.NewGuid(),
            Active = true,
            StoreId = CurrentStore.Id,
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
    ///     Set the current customer
    /// </summary>
    /// <returns></returns>
    public virtual async Task<Customer> SetCurrentCustomer(Customer customer)
    {
        if (customer == null || customer.Deleted || !customer.Active)
            //if the current customer is null/not active then set background task customer
            customer = await _customerService.GetCustomerBySystemName(SystemCustomerNames.BackgroundTask);

        return _cachedCustomer = customer ?? throw new Exception("No customer could be loaded");
    }

    /// <summary>
    ///     Gets the original customer
    /// </summary>
    public virtual Customer OriginalCustomerIfImpersonated => _originalCustomerIfImpersonated;

    /// <summary>
    ///     Gets the current vendor
    /// </summary>
    public virtual Vendor CurrentVendor => _cachedVendor;

    /// <summary>
    ///     Set the current vendor (logged-in manager)
    /// </summary>
    public virtual async Task<Vendor> SetCurrentVendor(Customer customer)
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
        return _cachedVendor = vendor;
    }

    /// <summary>
    ///     Gets or sets current user working language
    /// </summary>
    public virtual Language WorkingLanguage => _cachedLanguage;

    /// <summary>
    ///     Set current user working language by Middleware
    /// </summary>
    /// <param name="customer"></param>
    /// <returns></returns>
    public virtual async Task<Language> SetWorkingLanguage(Customer customer)
    {
        Language language = null;

        var adminAreaUrl =
            _httpContextAccessor.HttpContext?.Request.Path.StartsWithSegments(new PathString("/Admin"));
        var allStoreLanguages = await _languageService.GetAllLanguages(adminAreaUrl ?? false);

        if (allStoreLanguages.Count == 1)
            return _cachedLanguage = allStoreLanguages.FirstOrDefault();

        if (_config.SeoFriendlyUrlsForLanguagesEnabled)
            language = await GetLanguageFromUrl(allStoreLanguages);

        //get current lang identifier
        var customerLanguageId =
            customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LanguageId, CurrentStore.Id);

        if (_languageSettings.AutomaticallyDetectLanguage &&
            language == null && string.IsNullOrEmpty(customerLanguageId))
            language = await GetLanguageFromRequest(allStoreLanguages);

        //check customer language 
        var customerLanguage = (language ??
                                (!string.IsNullOrEmpty(customerLanguageId)
                                    ? allStoreLanguages.FirstOrDefault(lang => lang.Id == customerLanguageId)
                                    : allStoreLanguages.FirstOrDefault(lang =>
                                        //if the language for the current store not exist, then use the first
                                        lang.Id == CurrentStore.DefaultLanguageId))) ??
                               allStoreLanguages.FirstOrDefault();

        return _cachedLanguage = customerLanguage ?? throw new Exception("No language could be loaded");
    }

    /// <summary>
    ///     Get current user working currency
    /// </summary>
    public virtual Currency WorkingCurrency => _cachedCurrency;

    /// <summary>
    ///     Set current user working currency by Middleware
    /// </summary>
    public virtual async Task<Currency> SetWorkingCurrency(Customer customer)
    {
        //return store currency when we're you are in admin panel
        var adminAreaUrl =
            _httpContextAccessor.HttpContext?.Request.Path.StartsWithSegments(new PathString("/Admin"));
        if (adminAreaUrl.HasValue && adminAreaUrl.Value)
        {
            var primaryStoreCurrency = await _currencyService.GetPrimaryStoreCurrency();
            if (primaryStoreCurrency != null)
            {
                _cachedCurrency = primaryStoreCurrency;
                return primaryStoreCurrency;
            }
        }

        var allStoreCurrencies = await _currencyService.GetAllCurrencies(storeId: CurrentStore?.Id);

        if (allStoreCurrencies.Count == 1)
            return _cachedCurrency = allStoreCurrencies.FirstOrDefault();

        var customerCurrencyId =
            customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.CurrencyId, CurrentStore?.Id);

        var customerCurrency = (allStoreCurrencies.FirstOrDefault(currency => currency.Id == customerCurrencyId) ??
                                (!string.IsNullOrEmpty(CurrentStore?.DefaultCurrencyId)
                                    ? allStoreCurrencies.FirstOrDefault(currency =>
                                        currency.Id == CurrentStore.DefaultCurrencyId)
                                    : allStoreCurrencies.FirstOrDefault(currency =>
                                        currency.Id == WorkingLanguage.DefaultCurrencyId))) ??
                               allStoreCurrencies.FirstOrDefault();


        return _cachedCurrency = customerCurrency ?? throw new Exception("No currency could be loaded");
    }

    /// <summary>
    ///     Gets or sets current tax display type
    /// </summary>
    public virtual TaxDisplayType TaxDisplayType => _cachedTaxDisplayType;

    public virtual async Task<TaxDisplayType> SetTaxDisplayType(Customer customer)
    {
        TaxDisplayType taxDisplayType;

        if (_taxSettings.AllowCustomersToSelectTaxDisplayType && customer != null)
        {
            var taxDisplayTypeId =
                customer.GetUserFieldFromEntity<int>(SystemCustomerFieldNames.TaxDisplayTypeId, CurrentStore.Id);
            taxDisplayType = (TaxDisplayType)taxDisplayTypeId;
        }
        else
        {
            //or get the default tax display type
            taxDisplayType = _taxSettings.TaxDisplayType;
        }

        //cache the value
        _cachedTaxDisplayType = taxDisplayType;

        return await Task.FromResult(_cachedTaxDisplayType);
    }

    /// <summary>
    ///     Gets the current store
    /// </summary>
    public virtual Store CurrentStore => _storeHelper.StoreHost;

    /// <summary>
    ///     Gets the current domain host
    /// </summary>
    public virtual DomainHost CurrentHost => _storeHelper.DomainHost;

    #endregion
}