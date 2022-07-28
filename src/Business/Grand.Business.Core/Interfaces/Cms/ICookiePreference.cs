using Grand.Domain.Customers;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Interfaces.Cms
{
    public interface ICookiePreference
    {
        IList<IConsentCookie> GetConsentCookies();
        Task<bool?> IsEnable(Customer customer, Store store, string cookieSystemName);
    }
}
