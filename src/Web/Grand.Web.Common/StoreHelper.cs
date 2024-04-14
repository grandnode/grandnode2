using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Common;

public class StoreHelper : IStoreHelper
{
    #region Ctor

    public StoreHelper(
        IHttpContextAccessor httpContextAccessor,
        IStoreService storeService,
        SecurityConfig securityConfig)
    {
        _httpContextAccessor = httpContextAccessor;
        _storeService = storeService;
        _securityConfig = securityConfig;
    }

    #endregion

    public Store StoreHost {
        get {
            if (_cachedStore != null) return _cachedStore;
            //try to determine the current store by HOST header
            var host = _httpContextAccessor.HttpContext?.Request.Host.Host;

            var allStores = _storeService.GetAll();
            var stores = allStores.Where(s => s.ContainsHostValue(host)).ToList();
            if (!stores.Any())
                _cachedStore = allStores.FirstOrDefault();
            else
                switch (stores.Count)
                {
                    case 1:
                        _cachedStore = stores.FirstOrDefault();
                        break;
                    case > 1:
                    {
                        var cookie = GetStoreCookie();
                        if (!string.IsNullOrEmpty(cookie))
                        {
                            var storeCookie = stores.FirstOrDefault(x => x.Id == cookie);
                            _cachedStore = storeCookie ?? stores.FirstOrDefault();
                        }
                        else
                        {
                            _cachedStore = stores.FirstOrDefault();
                        }

                        break;
                    }
                }

            return _cachedStore ?? throw new Exception("No store could be loaded");
        }
    }

    public DomainHost DomainHost {
        get {
            if (_cachedDomainHost != null) return _cachedDomainHost;
            //try to determine the current HOST header
            var host = _httpContextAccessor.HttpContext?.Request.GetTypedHeaders().Host.ToString();
            if (StoreHost != null)
                _cachedDomainHost = StoreHost.HostValue(host) ?? new DomainHost {
                    Id = int.MinValue.ToString(),
                    Url = StoreHost.SslEnabled ? StoreHost.SecureUrl : StoreHost.Url,
                    HostName = "temporary-store"
                };

            return _cachedDomainHost ??= new DomainHost {
                Id = int.MinValue.ToString(),
                Url = host,
                HostName = "temporary"
            };
        }
    }

    /// <summary>
    ///     Set the current store by BackgroundService
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

        _cachedStore ??= (await _storeService.GetAllStores()).FirstOrDefault();

        return _cachedStore ?? throw new Exception("No store could be loaded by BackgroundService");
    }

    /// <summary>
    ///     Set store cookie
    /// </summary>
    /// <param name="storeId">Store ident</param>
    public virtual async Task SetStoreCookie(string storeId)
    {
        if (_httpContextAccessor.HttpContext == null)
            return;

        var store = await _storeService.GetStoreById(storeId);
        if (store == null)
            return;

        //remove current cookie
        _httpContextAccessor.HttpContext.Response.Cookies.Delete(StoreCookieName);

        //get date of cookie expiration
        var cookieExpiresDate = DateTime.UtcNow.AddHours(_securityConfig.CookieAuthExpires);

        //set new cookie value
        var options = new CookieOptions {
            HttpOnly = true,
            Expires = cookieExpiresDate
        };
        _httpContextAccessor.HttpContext.Response.Cookies.Append(StoreCookieName, storeId, options);
    }


    #region Utilities

    protected virtual string GetStoreCookie()
    {
        return _httpContextAccessor.HttpContext?.Request.Cookies[StoreCookieName];
    }

    #endregion

    #region Fields

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IStoreService _storeService;
    private readonly SecurityConfig _securityConfig;
    private Store _cachedStore;
    private DomainHost _cachedDomainHost;

    private const string StoreCookieName = ".Grand.Store";

    #endregion
}