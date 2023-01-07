﻿using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.ShoppingCart;
using MediatR;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Features.Handlers.ShoppingCart
{
    public class GetEstimateShippingHandler : IRequestHandler<GetEstimateShipping, EstimateShippingModel>
    {
        private readonly ITranslationService _translationService;
        private readonly ICountryService _countryService;

        private readonly ShippingSettings _shippingSettings;

        public GetEstimateShippingHandler(
            ITranslationService translationService,
            ICountryService countryService,
            ShippingSettings shippingSettings)
        {
            _translationService = translationService;
            _countryService = countryService;
            _shippingSettings = shippingSettings;
        }

        public async Task<EstimateShippingModel> Handle(GetEstimateShipping request, CancellationToken cancellationToken)
        {
            var model = new EstimateShippingModel {
                Enabled = request.Cart.Any() && request.Cart.RequiresShipping() && _shippingSettings.EstimateShippingEnabled
            };
            if (!model.Enabled) return model;
            
            //countries
            var defaultEstimateCountryId = request.SetEstimateShippingDefaultAddress && request.Customer.ShippingAddress != null ? request.Customer.ShippingAddress.CountryId : model.CountryId;
            if (string.IsNullOrEmpty(defaultEstimateCountryId))
                defaultEstimateCountryId = request.Store.DefaultCountryId;

            model.AvailableCountries.Add(new SelectListItem { Text = _translationService.GetResource("Address.SelectCountry"), Value = "" });
            foreach (var c in await _countryService.GetAllCountriesForShipping(request.Language.Id, request.Store.Id))
                model.AvailableCountries.Add(new SelectListItem {
                    Text = c.GetTranslation(x => x.Name, request.Language.Id),
                    Value = c.Id,
                    Selected = c.Id == defaultEstimateCountryId
                });
            //states
            var defaultEstimateStateId = request.SetEstimateShippingDefaultAddress && request.Customer.ShippingAddress != null ? request.Customer.ShippingAddress.StateProvinceId : model.StateProvinceId;
            var states = !string.IsNullOrEmpty(defaultEstimateCountryId) ? await _countryService.GetStateProvincesByCountryId(defaultEstimateCountryId, request.Language.Id) : new List<StateProvince>();
            if (states.Any())
                foreach (var s in states)
                    model.AvailableStates.Add(new SelectListItem {
                        Text = s.GetTranslation(x => x.Name, request.Language.Id),
                        Value = s.Id,
                        Selected = s.Id == defaultEstimateStateId
                    });

            if (request.SetEstimateShippingDefaultAddress && request.Customer.ShippingAddress != null)
                model.ZipPostalCode = request.Customer.ShippingAddress.ZipPostalCode;
            return model;
        }
    }
}
