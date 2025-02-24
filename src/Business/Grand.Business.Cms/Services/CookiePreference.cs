using Grand.Business.Core.Interfaces.Cms;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Stores;

namespace Grand.Business.Cms.Services;

public class CookiePreference : ICookiePreference
{
    private readonly IEnumerable<IConsentCookie> _consentCookies;

    public CookiePreference(
        IEnumerable<IConsentCookie> consentCookies)
    {
        _consentCookies = consentCookies;
    }

    public virtual IList<IConsentCookie> GetConsentCookies()
    {
        return _consentCookies.OrderBy(x => x.DisplayOrder).ToList();
    }


    public virtual Task<bool?> IsEnable(Customer customer, Store store, string cookieSystemName)
    {
        var result = default(bool?);
        var savedCookiesConsent = customer.GetUserFieldFromEntity<Dictionary<string, bool>>(SystemCustomerFieldNames.ConsentCookies, store.Id);
        if (savedCookiesConsent == null) return Task.FromResult<bool?>(null);
        if (savedCookiesConsent.TryGetValue(cookieSystemName, out var value))
            result = value;

        return Task.FromResult(result);
    }
}