using Grand.Domain.Customers;
using Grand.Domain.Stores;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Common.Interfaces.Directory
{
    public interface ICookiePreference
    {
        IList<IConsentCookie> GetConsentCookies();
        Task<bool?> IsEnable(Customer customer, Store store, string cookieSystemName);
    }
}
