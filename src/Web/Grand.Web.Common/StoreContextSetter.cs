using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Web.Common.Extensions;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Common;

public class StoreContextSetter : IStoreContextSetter
{
    private readonly IStoreService _storeService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public StoreContextSetter(IStoreService storeService, IHttpContextAccessor httpContextAccessor)
    {
        _storeService = storeService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IStoreContext> InitializeStoreContext(string storeId = null)
    {
        var store = await CurrentStore(storeId);
        var storeContext = new CurrentStoreContext {
            CurrentStore = store,
            CurrentHost = CurrentHost(store)
        };

        return storeContext;
    }
    protected async Task<Store> CurrentStore(string id = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        // Attempt to load store by provided id
        if (!string.IsNullOrEmpty(id))
        {
            var storeById = await _storeService.GetStoreById(id);
            if (storeById != null)
                return storeById;
        }

        // Attempt to load store by cookie value
        var storeCookie = httpContext?.GetStoreCookie();
        if (!string.IsNullOrEmpty(storeCookie))
        {
            var storeByCookie = await _storeService.GetStoreById(storeCookie);
            if (storeByCookie != null)
                return storeByCookie;
        }

        // Attempt to load store by host from request
        var host = httpContext?.Request.Host.Host;
        if (!string.IsNullOrEmpty(host))
        {
            var storeByHost = await _storeService.GetStoreByHost(host);
            if (storeByHost != null)
                return storeByHost;
        }

        // Fallback: return the first available store
        return (await _storeService.GetAllStores()).FirstOrDefault();
    }

    /// <summary>
    ///     Gets the current domain host
    /// </summary>
    protected DomainHost CurrentHost(Store store)
    {
        //try to determine the current HOST header
        var host = _httpContextAccessor.HttpContext?.Request.GetTypedHeaders().Host.ToString();
        if (store != null)
            return store.HostValue(host) ?? new DomainHost {
                Id = int.MinValue.ToString(),
                Url = store.SslEnabled ? store.SecureUrl : store.Url,
                HostName = "temporary-store"
            };

        return new DomainHost {
            Id = int.MinValue.ToString(),
            Url = host,
            HostName = "temporary"
        };
    }
    private sealed class CurrentStoreContext : IStoreContext
    {
        public Store CurrentStore { get; set; }

        public DomainHost CurrentHost { get; set; }
    }
}


