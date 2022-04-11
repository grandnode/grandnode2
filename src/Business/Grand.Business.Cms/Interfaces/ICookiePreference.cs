using Grand.Domain.Customers;
using Grand.Domain.Stores;

namespace Grand.Business.Cms.Interfaces
{
    public interface ICookiePreference
    {
        IList<IConsentCookie> GetConsentCookies();
        Task<bool?> IsEnable(Customer customer, Store store, string cookieSystemName);
    }
}
