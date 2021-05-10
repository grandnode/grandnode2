using Grand.Business.Common.Interfaces.Directory;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Common.Services.Directory
{
    public class CookiePreference : ICookiePreference
    {
        private readonly IUserFieldService _userFieldService;
        private readonly IEnumerable<IConsentCookie> _consentCookies;

        public CookiePreference(IUserFieldService userFieldService,
            IEnumerable<IConsentCookie> consentCookies)
        {
            _userFieldService = userFieldService;
            _consentCookies = consentCookies;
        }

        public virtual IList<IConsentCookie> GetConsentCookies()
        {
            return _consentCookies.OrderBy(x => x.DisplayOrder).ToList();
        }


        public virtual async Task<bool?> IsEnable(Customer customer, Store store, string cookieSystemName)
        {
            bool? result = default(bool?);
            var savedCookiesConsent = await _userFieldService.GetFieldsForEntity<Dictionary<string, bool>>(customer, SystemCustomerFieldNames.ConsentCookies, store.Id);
            if (savedCookiesConsent != null)
                if (savedCookiesConsent.ContainsKey(cookieSystemName))
                    result = savedCookiesConsent[cookieSystemName];

            return result;
        }
    }
}
