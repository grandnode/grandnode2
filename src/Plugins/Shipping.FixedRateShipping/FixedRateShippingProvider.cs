using Grand.Business.Checkout.Enum;
using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Business.Checkout.Utilities;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Http;
using Shipping.FixedRateShipping.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shipping.FixedRateShipping
{
    public class FixedRateShippingProvider : IShippingRateCalculationProvider
    {

        private readonly IShippingMethodService _shippingMethodService;
        private readonly IWorkContext _workContext;
        private readonly ISettingService _settingService;
        private readonly ICurrencyService _currencyService;
        private readonly ITranslationService _translationService;
        private readonly ShippingFixedRateSettings _shippingFixedRateSettings;


        public FixedRateShippingProvider(
            IShippingMethodService shippingMethodService,
            IWorkContext workContext,
            ISettingService settingService,
            ICurrencyService currencyService,
            ITranslationService translationService,
            ShippingFixedRateSettings shippingFixedRateSettings
            )
        {
            _shippingMethodService = shippingMethodService;
            _workContext = workContext;
            _settingService = settingService;
            _currencyService = currencyService;
            _translationService = translationService;
            _shippingFixedRateSettings = shippingFixedRateSettings;
        }
        #region Utilities

        private double GetRate(string shippingMethodId)
        {
            string key = string.Format("ShippingRateComputationMethod.FixedRate.Rate.ShippingMethodId{0}", shippingMethodId);
            var rate = this._settingService.GetSettingByKey<FixedShippingRate>(key)?.Rate;
            return rate ?? 0;
        }

        #endregion

        
        /// <summary>
        ///  Gets available shipping options
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Represents a response of getting shipping rate options</returns>
        public async Task<GetShippingOptionResponse> GetShippingOptions(GetShippingOptionRequest getShippingOptionRequest)
        {
            if (getShippingOptionRequest == null)
                throw new ArgumentNullException(nameof(getShippingOptionRequest));

            var response = new GetShippingOptionResponse();

            if (getShippingOptionRequest.Items == null || getShippingOptionRequest.Items.Count == 0)
            {
                response.AddError("No shipment items");
                return response;
            }

            string restrictByCountryId = (getShippingOptionRequest.ShippingAddress != null && !String.IsNullOrEmpty(getShippingOptionRequest.ShippingAddress.CountryId)) ? getShippingOptionRequest.ShippingAddress.CountryId : "";
            var shippingMethods = await _shippingMethodService.GetAllShippingMethods(restrictByCountryId, getShippingOptionRequest.Customer);
            foreach (var shippingMethod in shippingMethods)
            {
                var shippingOption = new ShippingOption
                {
                    Name = shippingMethod.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id),
                    Description = shippingMethod.GetTranslation(x => x.Description, _workContext.WorkingLanguage.Id),
                    Rate = await _currencyService.ConvertFromPrimaryStoreCurrency(GetRate(shippingMethod.Id), _workContext.WorkingCurrency)
                };
                response.ShippingOptions.Add(shippingOption);
            }

            return response;
        }

        /// <summary>
        /// Gets fixed shipping rate (if Shipping rate  method allows it and the rate can be calculated before checkout).
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Fixed shipping rate; or null in case there's no fixed shipping rate</returns>
        public async Task<double?> GetFixedRate(GetShippingOptionRequest getShippingOptionRequest)
        {
            if (getShippingOptionRequest == null)
                throw new ArgumentNullException(nameof(getShippingOptionRequest));

            string restrictByCountryId = (getShippingOptionRequest.ShippingAddress != null && !String.IsNullOrEmpty(getShippingOptionRequest.ShippingAddress.CountryId)) ? getShippingOptionRequest.ShippingAddress.CountryId : "";
            var shippingMethods = await _shippingMethodService.GetAllShippingMethods(restrictByCountryId);

            var rates = new List<double>();
            foreach (var shippingMethod in shippingMethods)
            {
                double rate = GetRate(shippingMethod.Id);
                if (!rates.Contains(rate))
                    rates.Add(rate);
            }

            //return default rate if all of them equal
            if (rates.Count == 1)
                return rates[0];

            return null;
        }

        public ShippingRateCalculationType ShippingRateCalculationType => ShippingRateCalculationType.Off;

        public IShipmentTracker ShipmentTracker => null;

        public async Task<IList<string>> ValidateShippingForm(IFormCollection form)
        {
            return await Task.FromResult(new List<string>());
        }

        public async Task<string> GetPublicViewComponentName()
        {
            return await Task.FromResult("");
        }

        public async Task<bool> HideShipmentMethods(IList<ShoppingCartItem> cart)
        {
            return await Task.FromResult(false);
        }
        public string ConfigurationUrl => FixedRateShippingDefaults.ConfigurationUrl;
        public string SystemName => FixedRateShippingDefaults.ProviderSystemName;
        public string FriendlyName => _translationService.GetResource(FixedRateShippingDefaults.FriendlyName);
        public int Priority => _shippingFixedRateSettings.DisplayOrder;
        public IList<string> LimitedToStores => new List<string>();
        public IList<string> LimitedToGroups => new List<string>();

    }
}
