using Grand.Business.Authentication.Interfaces;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Customers.Interfaces;
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
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wangkanai.Detection.Services;

namespace Grand.Web.Common
{
    /// <summary>
    /// Represents work context for web application / current customer / store / language / currency
    /// </summary>
    public partial class WorkContext : IWorkContext
    {
        #region Fields

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IGrandAuthenticationService _authenticationService;
        private readonly IApiAuthenticationService _apiauthenticationService;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly IGroupService _groupService;
        private readonly IUserFieldService _userFieldService;
        private readonly ILanguageService _languageService;
        private readonly IStoreHelper _storeHelper;
        private readonly IAclService _aclService;
        private readonly IVendorService _vendorService;
        private readonly IDetectionService _detectionService;

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

        #region Ctor

        public WorkContext(
            IHttpContextAccessor httpContextAccessor,
            IGrandAuthenticationService authenticationService,
            IApiAuthenticationService apiauthenticationService,
            ICurrencyService currencyService,
            ICustomerService customerService,
            IGroupService groupService,
            IUserFieldService userFieldService,
            ILanguageService languageService,
            IStoreHelper storeHelper,
            IAclService aclService,
            IVendorService vendorService,
            IDetectionService detectionService,
            LanguageSettings languageSettings,
            TaxSettings taxSettings,
            AppConfig config)
        {
            _httpContextAccessor = httpContextAccessor;
            _authenticationService = authenticationService;
            _apiauthenticationService = apiauthenticationService;
            _currencyService = currencyService;
            _customerService = customerService;
            _groupService = groupService;
            _userFieldService = userFieldService;
            _languageService = languageService;
            _storeHelper = storeHelper;
            _aclService = aclService;
            _vendorService = vendorService;
            _detectionService = detectionService;
            _languageSettings = languageSettings;
            _taxSettings = taxSettings;
            _config = config;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get language from the requested page URL
        /// </summary>
        /// <returns>The found language</returns>
        protected virtual async Task<Language> GetLanguageFromUrl(IList<Language> languages)
        {
            if (_httpContextAccessor.HttpContext == null || _httpContextAccessor.HttpContext.Request == null)
                return await Task.FromResult<Language>(null);

            var path = _httpContextAccessor.HttpContext.Request.Path.Value;
            if (string.IsNullOrEmpty(path))
                return await Task.FromResult<Language>(null);

            var firstSegment = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            if (string.IsNullOrEmpty(firstSegment))
                return await Task.FromResult<Language>(null);

            var language = languages.FirstOrDefault(urlLanguage => urlLanguage.UniqueSeoCode.Equals(firstSegment, StringComparison.OrdinalIgnoreCase));

            if (language == null || !language.Published || !_aclService.Authorize(language, CurrentStore.Id))
                return await Task.FromResult<Language>(null);

            return language;
        }

        /// <summary>
        /// Get language from the request
        /// </summary>
        /// <returns>The found language</returns>
        protected virtual async Task<Language> GetLanguageFromRequest(IList<Language> languages)
        {
            if (_httpContextAccessor.HttpContext == null || _httpContextAccessor.HttpContext.Request == null)
                return await Task.FromResult<Language>(null);

            //get request culture
            var requestCulture = _httpContextAccessor.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture;
            if (requestCulture == null)
                return await Task.FromResult<Language>(null);

            //try to get language by culture name
            var requestLanguage = languages.FirstOrDefault(language =>
                language.LanguageCulture.Equals(requestCulture.Culture.Name, StringComparison.OrdinalIgnoreCase));

            //check language availability
            if (requestLanguage == null || !requestLanguage.Published || !_aclService.Authorize(requestLanguage, CurrentStore.Id))
                return await Task.FromResult<Language>(null);

            return requestLanguage;
        }

        protected virtual async Task<Customer> SetImpersonatedCustomer(Customer customer)
        {
            //get impersonate user if required
            var impersonatedCustomerId = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.ImpersonatedCustomerId);
            if (!string.IsNullOrEmpty(impersonatedCustomerId))
            {
                var impersonatedCustomer = await _customerService.GetCustomerById(impersonatedCustomerId);
                if (impersonatedCustomer != null && !impersonatedCustomer.Deleted && impersonatedCustomer.Active)
                {
                    //set impersonated customer
                    _originalCustomerIfImpersonated = customer;
                    return impersonatedCustomer;
                }
            }
            return null;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current customer
        /// </summary>
        public virtual Customer CurrentCustomer => _cachedCustomer;

        /// <summary>
        /// Set the current customer by Middleware
        /// </summary>
        /// <returns></returns>
        public virtual async Task<Customer> SetCurrentCustomer()
        {
            Customer customer = null;
            //check whether request is made by a background (schedule) task
            if (_httpContextAccessor.HttpContext == null)
            {
                //in this case return built-in customer record for background task
                customer = await _customerService.GetCustomerBySystemName(SystemCustomerNames.BackgroundTask);
                //if customer comes from background task, doesn't need to create cookies
                if (customer != null)
                {
                    //cache the found customer
                    _cachedCustomer = customer;
                    return customer;
                }
            }

            //set customer as a background task if method setted as AllowAnonymous
            var endpoint = _httpContextAccessor.HttpContext?.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() is object)
            {
                customer = await _customerService.GetCustomerBySystemName(SystemCustomerNames.Anonymous);
                //if customer comes from Anonymous method, doesn't need to create cookies
                if (customer != null)
                {
                    //cache the found customer
                    _cachedCustomer = customer;
                    return customer;
                }
            }

            if (customer == null || customer.Deleted || !customer.Active)
            {
                //try to get registered user
                customer = await _authenticationService.GetAuthenticatedCustomer();
                //if customer is authenticated
                if (customer != null)
                {
                    //remove cookie
                    await _authenticationService.SetCustomerGuid(Guid.Empty);
                    
                    //set if use impersonated session
                    var impersonatedCustomer = await SetImpersonatedCustomer(customer);
                    if (impersonatedCustomer != null)
                        customer = impersonatedCustomer;

                    //cache the found customer
                    _cachedCustomer = customer;


                    return customer;
                }
            }

            if (customer == null)
            {
                //try to get api user
                customer = await _apiauthenticationService.GetAuthenticatedCustomer();
                //if customer comes from api, doesn't need to create cookies
                if (customer != null)
                {
                    //set if use impersonated session
                    var impersonatedCustomer = await SetImpersonatedCustomer(customer);
                    if (impersonatedCustomer != null)
                        customer = impersonatedCustomer;

                    //cache the found customer
                    _cachedCustomer = customer;
                    return customer;
                }
            }

            if (customer == null || customer.Deleted || !customer.Active)
            {
                //get guest customer
                var customerguid = await _authenticationService.GetCustomerGuid();
                if (!string.IsNullOrEmpty(customerguid))
                {
                    if (Guid.TryParse(customerguid, out Guid customerGuid))
                    {
                        //get customer from guid (cannot not be registered)
                        var customerByguid = await _customerService.GetCustomerByGuid(customerGuid);
                        if (customerByguid != null && !await _groupService.IsRegistered(customerByguid))
                            customer = customerByguid;
                    }
                }
            }

            if (customer == null || customer.Deleted || !customer.Active)
            {
                var isCrawler = _detectionService.Crawler?.IsCrawler;
                //check whether request is made by a search engine, in this case return built-in customer record for search engines
                if (isCrawler.HasValue && isCrawler.Value)
                    customer = await _customerService.GetCustomerBySystemName(SystemCustomerNames.SearchEngine);
            }

            if (customer == null || customer.Deleted || !customer.Active)
            {
                //create guest if not exists
                customer = await _customerService.InsertGuestCustomer(CurrentStore);
                string referrer = _httpContextAccessor?.HttpContext?.Request?.Headers[HeaderNames.Referer];
                if (!string.IsNullOrEmpty(referrer))
                {
                    await _userFieldService.SaveField(customer, SystemCustomerFieldNames.UrlReferrer, referrer);
                }
                //set customer cookie
                await _authenticationService.SetCustomerGuid(customer.CustomerGuid);
            }

            //cache the found customer
            return _cachedCustomer = customer ?? throw new Exception("No customer could be loaded");
        }

        /// <summary>
        /// Set the current customer
        /// </summary>
        /// <returns></returns>
        public virtual async Task<Customer> SetCurrentCustomer(Customer customer)
        {
            if (customer == null || customer.Deleted || !customer.Active)
            {
                //if the current customer is null/not active then set background task customer
                customer = await _customerService.GetCustomerBySystemName(SystemCustomerNames.BackgroundTask);
            }
            return _cachedCustomer = customer ?? throw new Exception("No customer could be loaded");
        }

        /// <summary>
        /// Gets the original customer
        /// </summary>
        public virtual Customer OriginalCustomerIfImpersonated => _originalCustomerIfImpersonated;

        /// <summary>
        /// Gets the current vendor
        /// </summary>
        public virtual Vendor CurrentVendor => _cachedVendor;

        /// <summary>
        /// Set the current vendor (logged-in manager)
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
        /// Gets or sets current user working language
        /// </summary>
        public virtual Language WorkingLanguage => _cachedLanguage;

        /// <summary>
        /// Set current user working language 
        /// </summary>
        public virtual async Task<Language> SetWorkingLanguage(Language language)
        {
            if (language != null)
                await _userFieldService.SaveField(CurrentCustomer, SystemCustomerFieldNames.LanguageId, language.Id, CurrentStore.Id);

            //then reset the cache value
            _cachedLanguage = null;

            return language;
        }

        /// <summary>
        /// Set current user working language by Middleware
        /// </summary>
        /// <param name="customer"></param>
        /// <returns></returns>
        public virtual async Task<Language> SetWorkingLanguage(Customer customer)
        {
            Language language = null;

            var adminAreaUrl = _httpContextAccessor.HttpContext.Request.Path.StartsWithSegments(new PathString("/Admin"));
            var allStoreLanguages = await _languageService.GetAllLanguages(showHidden: adminAreaUrl);

            if (allStoreLanguages.Count() == 1)
                return _cachedLanguage = allStoreLanguages.FirstOrDefault();

            if (_config.SeoFriendlyUrlsForLanguagesEnabled)
                language = await GetLanguageFromUrl(allStoreLanguages);

            //get current lang identifier
            var customerLanguageId = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LanguageId, CurrentStore.Id);

            if (_languageSettings.AutomaticallyDetectLanguage &&
                language == null && string.IsNullOrEmpty(customerLanguageId))
            {
                language = await GetLanguageFromRequest(allStoreLanguages);
            }

            //check customer language 
            var customerLanguage = language ??
                (!string.IsNullOrEmpty(customerLanguageId) ?
                allStoreLanguages.FirstOrDefault(language => language.Id == customerLanguageId) :
                allStoreLanguages.FirstOrDefault(language => language.Id == CurrentStore.DefaultLanguageId));

            //if the language for the current store not exist, then use the first
            if (customerLanguage == null)
                customerLanguage = allStoreLanguages.FirstOrDefault();

            //cache the language
            _cachedLanguage = customerLanguage;

            //save the language identifier
            if (customerLanguage.Id != customerLanguageId)
            {
                await _userFieldService.SaveField(customer,
                    SystemCustomerFieldNames.LanguageId, customerLanguage?.Id, CurrentStore.Id);
            }

            return _cachedLanguage ?? throw new Exception("No language could be loaded");
        }

        /// <summary>
        /// Get current user working currency
        /// </summary>
        public virtual Currency WorkingCurrency => _cachedCurrency;

        /// <summary>
        /// Set current user working currency by Middleware
        /// </summary>
        public virtual async Task<Currency> SetWorkingCurrency(Customer customer)
        {
            //return store currency when we're you are in admin panel
            var adminAreaUrl = _httpContextAccessor.HttpContext.Request.Path.StartsWithSegments(new PathString("/Admin"));
            if (adminAreaUrl)
            {
                var primaryStoreCurrency = await _currencyService.GetPrimaryStoreCurrency();
                if (primaryStoreCurrency != null)
                {
                    _cachedCurrency = primaryStoreCurrency;
                    return primaryStoreCurrency;
                }
            }
            var allStoreCurrencies = await _currencyService.GetAllCurrencies(storeId: CurrentStore?.Id);

            if (allStoreCurrencies.Count() == 1)
                return _cachedCurrency = allStoreCurrencies.FirstOrDefault();

            var customerCurrencyId = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.CurrencyId, CurrentStore.Id);

            var customerCurrency = allStoreCurrencies.FirstOrDefault(currency => currency.Id == customerCurrencyId);

            if (customerCurrency == null)
            {
                if (!string.IsNullOrEmpty(CurrentStore?.DefaultCurrencyId))
                    customerCurrency = allStoreCurrencies.FirstOrDefault(currency => currency.Id == CurrentStore.DefaultCurrencyId);
                else
                    customerCurrency = allStoreCurrencies.FirstOrDefault(currency => currency.Id == WorkingLanguage.DefaultCurrencyId);
            }

            if (customerCurrency == null)
                customerCurrency = allStoreCurrencies.FirstOrDefault();

            //cache the currency
            _cachedCurrency = customerCurrency;

            //save the currency identifier
            if (customerCurrency.Id != customerCurrencyId)
            {
                await _userFieldService.SaveField(customer,
                    SystemCustomerFieldNames.CurrencyId, customerCurrency?.Id, CurrentStore.Id);
            }

            return _cachedCurrency ?? throw new Exception("No currency could be loaded");
        }

        /// <summary>
        /// Set user working currency
        /// </summary>
        public virtual async Task<Currency> SetWorkingCurrency(Currency currency)
        {
            //and save it
            await _userFieldService.SaveField(CurrentCustomer,
                SystemCustomerFieldNames.CurrencyId, currency.Id, CurrentStore.Id);

            //then reset the cache value
            _cachedCurrency = null;

            return currency;
        }

        /// <summary>
        /// Gets or sets current tax display type
        /// </summary>
        public virtual TaxDisplayType TaxDisplayType => _cachedTaxDisplayType;

        public virtual async Task<TaxDisplayType> SetTaxDisplayType(Customer customer)
        {
            TaxDisplayType taxDisplayType;

            if (_taxSettings.AllowCustomersToSelectTaxDisplayType && customer != null)
            {
                var taxDisplayTypeId = customer.GetUserFieldFromEntity<int>(SystemCustomerFieldNames.TaxDisplayTypeId, CurrentStore.Id);
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


        public virtual async Task<TaxDisplayType> SetTaxDisplayType(TaxDisplayType taxDisplayType)
        {
            //whether customers are allowed to select tax display type
            if (!_taxSettings.AllowCustomersToSelectTaxDisplayType)
                return await Task.FromResult(taxDisplayType);

            //save passed value
            await _userFieldService.SaveField(CurrentCustomer,
                SystemCustomerFieldNames.TaxDisplayTypeId, (int)taxDisplayType, CurrentStore.Id);

            //then reset the cache value
            _cachedTaxDisplayType = taxDisplayType;
            return taxDisplayType;
        }

        /// <summary>
        /// Gets the current store
        /// </summary>
        public virtual Store CurrentStore => _storeHelper.HostStore;

        #endregion
    }
}
