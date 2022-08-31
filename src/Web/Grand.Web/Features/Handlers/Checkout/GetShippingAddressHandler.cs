using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Common;
using Grand.Domain.Shipping;
using Grand.Web.Features.Models.Checkout;
using Grand.Web.Features.Models.Common;
using Grand.Web.Models.Checkout;
using MediatR;

namespace Grand.Web.Features.Handlers.Checkout
{
    public class GetShippingAddressHandler : IRequestHandler<GetShippingAddress, CheckoutShippingAddressModel>
    {
        private readonly IShippingService _shippingService;
        private readonly IPickupPointService _pickupPointService;
        private readonly ITaxService _taxService;
        private readonly ICurrencyService _currencyService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly ITranslationService _translationService;
        private readonly ICountryService _countryService;
        private readonly IAclService _aclService;
        private readonly IMediator _mediator;
        private readonly ShippingSettings _shippingSettings;
        private readonly AddressSettings _addressSettings;

        public GetShippingAddressHandler(
            IShippingService shippingService,
            IPickupPointService pickupPointService,
            ITaxService taxService,
            ICurrencyService currencyService,
            IPriceFormatter priceFormatter,
            ITranslationService translationService,
            ICountryService countryService,
            IAclService aclService,
            IMediator mediator,
            ShippingSettings shippingSettings,
            AddressSettings addressSettings)
        {
            _shippingService = shippingService;
            _pickupPointService = pickupPointService;
            _taxService = taxService;
            _currencyService = currencyService;
            _priceFormatter = priceFormatter;
            _translationService = translationService;
            _countryService = countryService;
            _aclService = aclService;
            _mediator = mediator;
            _shippingSettings = shippingSettings;
            _addressSettings = addressSettings;
        }

        public async Task<CheckoutShippingAddressModel> Handle(GetShippingAddress request, CancellationToken cancellationToken)
        {
            var model = new CheckoutShippingAddressModel();
            model.BillToTheSameAddress = true;
            //allow pickup in store?
            model.AllowPickUpInStore = _shippingSettings.AllowPickUpInStore;
            if (model.AllowPickUpInStore)
            {
                await PreparePickupPoints(model, request);
            }

            await PrepareAddresses(model, request);

            //new address
            model.NewAddress.CountryId = request.SelectedCountryId;
            var countries = await _countryService.GetAllCountriesForShipping(request.Language.Id, request.Store.Id);
            model.NewAddress = await _mediator.Send(new GetAddressModel() {
                Language = request.Language,
                Store = request.Store,
                Model = model.NewAddress,
                Address = null,
                ExcludeProperties = false,
                LoadCountries = () => countries,
                PrePopulateWithCustomerFields = request.PrePopulateNewAddressWithCustomerFields,
                Customer = request.Customer,
                OverrideAttributes = request.OverrideAttributes,
            });
            model.NewAddress.HideAddressType = true;
            model.NewAddress.AddressTypeId = _addressSettings.AddressTypeEnabled ? (int)AddressType.Shipping : (int)AddressType.Any;

            return model;
        }

        private async Task PreparePickupPoints(CheckoutShippingAddressModel model, GetShippingAddress request)
        {
            var pickupPoints = await _pickupPointService.LoadActivePickupPoints(request.Store.Id);

            if (pickupPoints.Any())
            {
                foreach (var pickupPoint in pickupPoints)
                {
                    var pickupPointModel = new CheckoutPickupPointModel() {
                        Id = pickupPoint.Id,
                        Name = pickupPoint.Name,
                        Description = pickupPoint.Description,
                        Address = pickupPoint.Address,
                    };
                    if (pickupPoint.PickupFee > 0)
                    {
                        var amount = (await _taxService.GetShippingPrice(pickupPoint.PickupFee, request.Customer)).shippingPrice;
                        amount = await _currencyService.ConvertFromPrimaryStoreCurrency(amount, request.Currency);
                        pickupPointModel.PickupFee = _priceFormatter.FormatShippingPrice(amount);
                    }
                    model.PickupPoints.Add(pickupPointModel);
                }
            }

            if (!(await _shippingService.LoadActiveShippingRateCalculationProviders(request.Customer, request.Store.Id)).Any())
            {
                if (!pickupPoints.Any())
                {
                    model.Warnings.Add(_translationService.GetResource("Checkout.ShippingIsNotAllowed"));
                    model.Warnings.Add(_translationService.GetResource("Checkout.PickupPoints.NotAvailable"));
                }
                model.PickUpInStoreOnly = true;
                model.PickUpInStore = true;
            }
        }

        private async Task PrepareAddresses(CheckoutShippingAddressModel model, GetShippingAddress request)
        {
            //existing addresses
            var addresses = new List<Address>();
            foreach (var item in request.Customer.Addresses.Where(x => x.AddressType == AddressType.Any || x.AddressType == AddressType.Shipping))
            {
                if (string.IsNullOrEmpty(item.CountryId))
                {
                    addresses.Add(item);
                    continue;
                }
                var country = await _countryService.GetCountryById(item.CountryId);
                if (country == null || (country.AllowsShipping && _aclService.Authorize(country, request.Store.Id)))
                {
                    addresses.Add(item);
                    continue;
                }
            }
            foreach (var address in addresses)
            {
                var addressModel = await _mediator.Send(new GetAddressModel() {
                    Language = request.Language,
                    Store = request.Store,
                    Model = null,
                    Address = address,
                    ExcludeProperties = false,
                });
                model.ExistingAddresses.Add(addressModel);
            }
        }
    }
}
