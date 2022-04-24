using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Grand.Web.Common
{
    public class StoreHelper : IStoreHelper
    {
        #region Fields

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IStoreService _storeService;

        private Store _cachedStore;
        private DomainHost _cachedDomainHost;

        private const string STORE_COOKIE_NAME = ".Grand.Store";

        #endregion

        #region Ctor

        public StoreHelper(
            IHttpContextAccessor httpContextAccessor,
            IStoreService storeService)
        {
            _httpContextAccessor = httpContextAccessor;
            _storeService = storeService;
        }

        #endregion


        #region Utilities

        protected virtual string GetStoreCookie()
        {
            if (_httpContextAccessor.HttpContext == null || _httpContextAccessor.HttpContext.Request == null)
                return null;

            return _httpContextAccessor.HttpContext.Request.Cookies[STORE_COOKIE_NAME];
        }
        #endregion

        public Store StoreHost {
            get {
                if (_cachedStore == null)
                {
                    //try to determine the current store by HOST header
                    string host = _httpContextAccessor.HttpContext?.Request?.Host.Host;

                    var allStores = _storeService.GetAll();
                    var stores = allStores.Where(s => s.ContainsHostValue(host));
                    if (stores.Count() == 0)
                    {
                        _cachedStore = allStores.FirstOrDefault();
                    }
                    else if (stores.Count() == 1)
                    {
                        _cachedStore = stores.FirstOrDefault();
                    }
                    else if (stores.Count() > 1)
                    {
                        var cookie = GetStoreCookie();
                        if (!string.IsNullOrEmpty(cookie))
                        {
                            var storecookie = stores.FirstOrDefault(x => x.Id == cookie);
                            if (storecookie != null)
                                _cachedStore = storecookie;
                            else
                                _cachedStore = stores.FirstOrDefault();
                        }
                        else
                            _cachedStore = stores.FirstOrDefault();
                    }
                    return _cachedStore ?? throw new Exception("No store could be loaded");


                }
                return _cachedStore;
            }
        }

        public DomainHost DomainHost {
            get 
                {
                    if (_cachedDomainHost == null)
                    {
                        //try to determine the current HOST header
                        string host = _httpContextAccessor.HttpContext?.Request?.Headers[HeaderNames.Host];
                        if (StoreHost != null)
                        {
                            _cachedDomainHost = StoreHost.HostValue(host) ?? new DomainHost() {
                                Id = int.MinValue.ToString(),
                                Url = StoreHost.SslEnabled ? StoreHost.SecureUrl : StoreHost.Url,
                                HostName = "temporary-store"
                            };
                        }
                        if (_cachedDomainHost == null)
                        {
                            _cachedDomainHost = new DomainHost() {
                                Id = int.MinValue.ToString(),
                                Url = host,
                                HostName = "temporary"
                            };
                        }
                        return _cachedDomainHost;
                    }
                    return _cachedDomainHost;
                }
        }

        /// <summary>
        /// Set the current store by BackgroundService
        /// </summary>
        /// <returns></returns>
        public virtual async Task<Store> SetCurrentStore(string storeId)
        {
            if (!string.IsNullOrEmpty(storeId))
            {
                var store = await _storeService.GetStoreById(storeId);
                if (store != null)
                    _cachedStore = store;
            }
            if (_cachedStore == null)
                _cachedStore = (await _storeService.GetAllStores()).FirstOrDefault();

            return _cachedStore ?? throw new Exception("No store could be loaded by BackgroundService");

        }

        /// <summary>
        /// Set store cookie
        /// </summary>
        /// <param name="storeId">Store ident</param>
        public virtual async Task SetStoreCookie(string storeId)
        {
            if (_httpContextAccessor.HttpContext == null || _httpContextAccessor.HttpContext.Response == null)
                return;

            var store = await _storeService.GetStoreById(storeId);
            if (store == null)
                return;

            //remove current cookie
            _httpContextAccessor.HttpContext.Response.Cookies.Delete(STORE_COOKIE_NAME);

            //get date of cookie expiration
            var cookieExpiresDate = DateTime.UtcNow.AddHours(CommonHelper.CookieAuthExpires);

            //set new cookie value
            var options = new CookieOptions {
                HttpOnly = true,
                Expires = cookieExpiresDate
            };
            _httpContextAccessor.HttpContext.Response.Cookies.Append(STORE_COOKIE_NAME, storeId, options);
        }

    }
}
