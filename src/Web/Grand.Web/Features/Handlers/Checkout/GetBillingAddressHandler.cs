﻿using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Common;
using Grand.Web.Features.Models.Checkout;
using Grand.Web.Features.Models.Common;
using Grand.Web.Models.Checkout;
using MediatR;

namespace Grand.Web.Features.Handlers.Checkout
{
    public class GetBillingAddressHandler : IRequestHandler<GetBillingAddress, CheckoutBillingAddressModel>
    {
        private readonly ICountryService _countryService;
        private readonly IAclService _aclService;
        private readonly IMediator _mediator;
        private readonly AddressSettings _addressSettings;

        public GetBillingAddressHandler(
            ICountryService countryService,
            IAclService aclService,
            IMediator mediator,
            AddressSettings addressSettings)
        {
            _countryService = countryService;
            _aclService = aclService;
            _mediator = mediator;
            _addressSettings = addressSettings;
        }

        public async Task<CheckoutBillingAddressModel> Handle(GetBillingAddress request, CancellationToken cancellationToken)
        {
            var model = new CheckoutBillingAddressModel();
            //existing addresses
            var addresses = new List<Address>();
            foreach (var item in request.Customer.Addresses.Where(x => x.AddressType is AddressType.Any or AddressType.Billing))
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
                }
            }

            foreach (var address in addresses)
            {
                var addressModel = await _mediator.Send(new GetAddressModel {
                    Language = request.Language,
                    Store = request.Store,
                    Model = null,
                    Address = address,
                    ExcludeProperties = false
                }, cancellationToken);
                model.ExistingAddresses.Add(addressModel);
            }

            //new address
            model.BillingNewAddress.CountryId = request.SelectedCountryId;
            var countries = await _countryService.GetAllCountriesForBilling(request.Language.Id, request.Store.Id);

            model.BillingNewAddress = await _mediator.Send(new GetAddressModel {
                Language = request.Language,
                Store = request.Store,
                Model = model.BillingNewAddress,
                Address = null,
                ExcludeProperties = false,
                PrePopulateWithCustomerFields = request.PrePopulateNewAddressWithCustomerFields,
                LoadCountries = () => countries,
                Customer = request.Customer,
                OverrideAttributes = request.OverrideAttributes
            }, cancellationToken);
            model.BillingNewAddress.HideAddressType = true;
            model.BillingNewAddress.AddressTypeId = _addressSettings.AddressTypeEnabled ? (int)AddressType.Billing : (int)AddressType.Any;

            return model;
        }
    }
}
