using Grand.Business.Common.Interfaces.Directory;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Web.Features.Models.Common;
using Grand.Web.Models.Common;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Common
{
    public class GetPrivacyPreferenceModelHandler : IRequestHandler<GetPrivacyPreference, IList<PrivacyPreferenceModel>>
    {
        private readonly IUserFieldService _userFieldService;
        private readonly ICookiePreference _cookiePreference;

        public GetPrivacyPreferenceModelHandler(
            IUserFieldService userFieldService,
            ICookiePreference cookiePreference)
        {
            _userFieldService = userFieldService;
            _cookiePreference = cookiePreference;
        }

        public async Task<IList<PrivacyPreferenceModel>> Handle(GetPrivacyPreference request, CancellationToken cancellationToken)
        {
            var model = new List<PrivacyPreferenceModel>();
            var consentCookies = _cookiePreference.GetConsentCookies();
            var savedCookiesConsent = request.Customer.GetUserFieldFromEntity<Dictionary<string, bool>>(SystemCustomerFieldNames.ConsentCookies, request.Store.Id);
               
            foreach (var item in consentCookies)
            {
                var state = item.DefaultState ?? false;
                if (savedCookiesConsent != null && savedCookiesConsent.ContainsKey(item.SystemName))
                    state = savedCookiesConsent[item.SystemName];

                var privacyPreferenceModel = new PrivacyPreferenceModel
                {
                    Description = await item.FullDescription(),
                    Name = await item.Name(),
                    SystemName = item.SystemName,
                    AllowToDisable = item.AllowToDisable,
                    State = state,
                    DisplayOrder = item.DisplayOrder
                };
                model.Add(privacyPreferenceModel);
            }
            return model;
        }
    }
}
