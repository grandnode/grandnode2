using Grand.Business.Checkout.Enum;
using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Business.Checkout.Utilities;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Http;
using Shipping.ShippingPoint.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Shipping.ShippingPoint
{
    public class ShippingPointRateProvider : IShippingRateCalculationProvider
    {
        #region Fields

        private readonly IShippingPointService _shippingPointService;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;
        private readonly IUserFieldService _userFieldService;
        private readonly ICountryService _countryService;
        private readonly ICurrencyService _currencyService;

        private readonly ShippingPointRateSettings _shippingPointRateSettings;

        #endregion

        #region Ctor
        public ShippingPointRateProvider(
            IShippingPointService shippingPointService,
            ITranslationService translationService,
            IWorkContext workContext,
            IUserFieldService userFieldService,
            ICountryService countryService,
            ICurrencyService currencyService,
            ShippingPointRateSettings shippingPointRateSettings
            )
        {
            _shippingPointService = shippingPointService;
            _translationService = translationService;
            _workContext = workContext;
            _userFieldService = userFieldService;
            _countryService = countryService;
            _currencyService = currencyService;
            _shippingPointRateSettings = shippingPointRateSettings;
        }
        #endregion

        #region Methods

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


            response.ShippingOptions.Add(new ShippingOption()
            {
                Name = _translationService.GetResource("Shipping.ShippingPoint.PluginName"),
                Description = _translationService.GetResource("Shipping.ShippingPoint.PluginDescription"),
                Rate = 0,
                ShippingRateProviderSystemName = "Shipping.ShippingPoint"
            });

            return await Task.FromResult(response);
        }

        /// <summary>
        /// Gets fixed shipping rate (if Shipping rate  method allows it and the rate can be calculated before checkout).
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Fixed shipping rate; or null in case there's no fixed shipping rate</returns>
        public Task<double?> GetFixedRate(GetShippingOptionRequest getShippingOptionRequest)
        {
            return Task.FromResult(default(double?));
        }

        public async Task<bool> HideShipmentMethods(IList<ShoppingCartItem> cart)
        {
            return await Task.FromResult(false);
        }

        public async Task<IList<string>> ValidateShippingForm(IFormCollection form)
        {
            var shippingMethodName = form["shippingoption"].ToString().Replace("___", "_").Split(new[] { '_' })[0];
            var shippingOptionId = form["selectedShippingOption"].ToString();

            if (string.IsNullOrEmpty(shippingOptionId))
                return new List<string>() { _translationService.GetResource("Shipping.ShippingPoint.SelectBeforeProceed") };

            if (shippingMethodName != _translationService.GetResource("Shipping.ShippingPoint.PluginName"))
                throw new ArgumentException("shippingMethodName");

            var chosenShippingOption = await _shippingPointService.GetStoreShippingPointById(shippingOptionId);
            if (chosenShippingOption == null)
                return new List<string>() { _translationService.GetResource("Shipping.ShippingPoint.SelectBeforeProceed") };

            //override price 
            var offeredShippingOptions = await _workContext.CurrentCustomer.GetUserField<List<ShippingOption>>(_userFieldService, SystemCustomerFieldNames.OfferedShippingOptions, _workContext.CurrentStore.Id);
            offeredShippingOptions.Find(x => x.Name == shippingMethodName).Rate = await _currencyService.ConvertFromPrimaryStoreCurrency(chosenShippingOption.PickupFee, _workContext.WorkingCurrency);

            await _userFieldService.SaveField(
                _workContext.CurrentCustomer,
                SystemCustomerFieldNames.OfferedShippingOptions,
                offeredShippingOptions,
                _workContext.CurrentStore.Id);

            var forCustomer =
            string.Format("<strong>{0}:</strong> {1}<br><strong>{2}:</strong> {3}<br>",
                _translationService.GetResource("Shipping.ShippingPoint.Fields.ShippingPointName"), chosenShippingOption.ShippingPointName,
                _translationService.GetResource("Shipping.ShippingPoint.Fields.Description"), chosenShippingOption.Description
            );

            await _userFieldService.SaveField(
                _workContext.CurrentCustomer,
                SystemCustomerFieldNames.ShippingOptionAttributeDescription,
                forCustomer,
                    _workContext.CurrentStore.Id);

            var serializedObject = new Domain.ShippingPointSerializable()
            {
                Id = chosenShippingOption.Id,
                ShippingPointName = chosenShippingOption.ShippingPointName,
                Description = chosenShippingOption.Description,
                OpeningHours = chosenShippingOption.OpeningHours,
                PickupFee = chosenShippingOption.PickupFee,
                Country = (await _countryService.GetCountryById(chosenShippingOption.CountryId))?.Name,
                City = chosenShippingOption.City,
                Address1 = chosenShippingOption.Address1,
                ZipPostalCode = chosenShippingOption.ZipPostalCode,
                StoreId = chosenShippingOption.StoreId,
            };

            var stringBuilder = new StringBuilder();
            string serializedAttribute;
            using (var tw = new StringWriter(stringBuilder))
            {
                var xmlS = new XmlSerializer(typeof(Domain.ShippingPointSerializable));
                xmlS.Serialize(tw, serializedObject);
                serializedAttribute = stringBuilder.ToString();
            }

            await _userFieldService.SaveField(_workContext.CurrentCustomer,
                SystemCustomerFieldNames.ShippingOptionAttribute,
                    serializedAttribute,
                    _workContext.CurrentStore.Id);

            return new List<string>();
        }

        public async Task<string> GetPublicViewComponentName()
        {
            return await Task.FromResult("ShippingPoint");
        }

        public ShippingRateCalculationType ShippingRateCalculationType => ShippingRateCalculationType.Off;

        public IShipmentTracker ShipmentTracker => null;

        public string ConfigurationUrl => ShippingPointRateDefaults.ConfigurationUrl;

        public string SystemName => ShippingPointRateDefaults.ProviderSystemName;

        public string FriendlyName => _translationService.GetResource(ShippingPointRateDefaults.FriendlyName);

        public int Priority => _shippingPointRateSettings.DisplayOrder;

        public IList<string> LimitedToStores => new List<string>();

        public IList<string> LimitedToGroups => new List<string>();

        #endregion

    }
}
