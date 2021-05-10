using Grand.Business.Checkout.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Domain.Common;
using Grand.Domain.Shipping;
using Grand.Web.Features.Models.Checkout;
using Grand.Web.Features.Models.Common;
using Grand.Web.Models.Checkout;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Checkout
{
    public class GetBillingAddressHandler : IRequestHandler<GetBillingAddress, CheckoutBillingAddressModel>
    {
        private readonly ShippingSettings _shippingSettings;
        private readonly ICountryService _countryService;
        private readonly IAclService _aclService;
        private readonly IMediator _mediator;

        public GetBillingAddressHandler(ShippingSettings shippingSettings,
            ICountryService countryService,
            IAclService aclService,
            IMediator mediator)
        {
            _shippingSettings = shippingSettings;
            _countryService = countryService;
            _aclService = aclService;
            _mediator = mediator;
        }

        public async Task<CheckoutBillingAddressModel> Handle(GetBillingAddress request, CancellationToken cancellationToken)
        {
            var model = new CheckoutBillingAddressModel();
            //existing addresses
            var addresses = new List<Address>();
            foreach (var item in request.Customer.Addresses.Where(x=>x.AddressType == AddressType.Any || x.AddressType == AddressType.Billing))
            {
                if (string.IsNullOrEmpty(item.CountryId))
                {
                    addresses.Add(item);
                    continue;
                }
                var country = await _countryService.GetCountryById(item.CountryId);
                if (country == null || (country.AllowsBilling && _aclService.Authorize(country, request.Store.Id)))
                {
                    addresses.Add(item);
                    continue;
                }
            }

            foreach (var address in addresses)
            {
                var addressModel = await _mediator.Send(new GetAddressModel()
                {
                    Language = request.Language,
                    Store = request.Store,
                    Model = null,
                    Address = address,
                    ExcludeProperties = false,
                });
                model.ExistingAddresses.Add(addressModel);
            }

            //new address
            model.NewAddress.CountryId = request.SelectedCountryId;
            var countries = await _countryService.GetAllCountriesForBilling(request.Language.Id, request.Store.Id);

            model.NewAddress = await _mediator.Send(new GetAddressModel()
            {
                Language = request.Language,
                Store = request.Store,
                Model = model.NewAddress,
                Address = null,
                ExcludeProperties = false,
                PrePopulateWithCustomerFields = request.PrePopulateNewAddressWithCustomerFields,
                LoadCountries = () => countries,
                Customer = request.Customer,
                OverrideAttributes = request.OverrideAttributes
            });

            return model;
        }
    }
}
