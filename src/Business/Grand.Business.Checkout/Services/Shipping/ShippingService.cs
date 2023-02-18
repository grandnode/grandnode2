using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel;

namespace Grand.Business.Checkout.Services.Shipping
{
    /// <summary>
    /// Shipping service
    /// </summary>
    public class ShippingService : IShippingService
    {
        #region Fields

        private readonly ILogger _logger;
        private readonly ITranslationService _translationService;
        private readonly ICountryService _countryService;
        private readonly IEnumerable<IShippingRateCalculationProvider> _shippingRateCalculationProvider;
        private readonly ShippingSettings _shippingSettings;
        private readonly ShippingProviderSettings _shippingProviderSettings;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        public ShippingService(
            ILogger logger,
            ITranslationService translationService,
            ICountryService countryService,
            IEnumerable<IShippingRateCalculationProvider> shippingRateCalculationProvider,
            ShippingProviderSettings shippingProviderSettings,
            ShippingSettings shippingSettings)
        {
            _logger = logger;
            _translationService = translationService;
            _countryService = countryService;
            _shippingRateCalculationProvider = shippingRateCalculationProvider;
            _shippingProviderSettings = shippingProviderSettings;
            _shippingSettings = shippingSettings;
        }

        #endregion

        #region Shipping rate  methods

        /// <summary>
        /// Load active Shipping rate  methods
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="storeId">Store ident</param>
        /// <param name="cart">Cart</param>
        /// <returns>Shipping rate  methods</returns>
        public virtual async Task<IList<IShippingRateCalculationProvider>> LoadActiveShippingRateCalculationProviders(Customer customer = null, string storeId = "", IList<ShoppingCartItem> cart = null)
        {
            var shippingMethods = LoadAllShippingRateCalculationProviders(customer, storeId)
                   .Where(provider => _shippingProviderSettings.ActiveSystemNames.Contains(provider.SystemName, StringComparer.OrdinalIgnoreCase))
                   .ToList();

            var availableShippingMethods = new List<IShippingRateCalculationProvider>();
            foreach (var sm in shippingMethods)
            {
                if (!await sm.HideShipmentMethods(cart))
                    availableShippingMethods.Add(sm);
            }
            return availableShippingMethods;
        }

        /// <summary>
        /// Load Shipping rate  method by system name
        /// </summary>
        /// <param name="systemName">System name</param>
        /// <returns>Found Shipping rate  method</returns>
        public virtual IShippingRateCalculationProvider LoadShippingRateCalculationProviderBySystemName(string systemName)
        {
            return _shippingRateCalculationProvider.FirstOrDefault(x => x.SystemName == systemName);
        }

        /// <summary>
        /// Load all Shipping rate  methods
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="storeId">Store ident</param>
        /// <returns>Shipping rate  methods</returns>
        public virtual IList<IShippingRateCalculationProvider> LoadAllShippingRateCalculationProviders(Customer customer = null, string storeId = "")
        {
            return _shippingRateCalculationProvider
                    .Where(x =>
                    x.IsAuthenticateStore(storeId) &&
                    x.IsAuthenticateGroup(customer)).OrderBy(x => x.Priority).ToList();
        }

        #endregion


        #region Workflow

        /// <summary>
        /// Create shipment packages (requests) from shopping cart
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="cart">Shopping cart</param>
        /// <param name="shippingAddress">Shipping address</param>
        /// <param name="store">Store</param>
        /// <returns>Shipment packages (requests)</returns>
        public virtual async Task<GetShippingOptionRequest> CreateShippingOptionRequests(Customer customer,
            IList<ShoppingCartItem> cart, Address shippingAddress, Store store)
        {
            var request = new GetShippingOptionRequest {
                //store
                StoreId = store?.Id,
                //customer
                Customer = customer,

                //ship to
                ShippingAddress = shippingAddress
            };
            //ship from
            Address originAddress = _shippingSettings.ShippingOriginAddress;

            if (originAddress != null)
            {
                var country = await _countryService.GetCountryById(originAddress.CountryId);
                var state = country?.StateProvinces.FirstOrDefault(x => x.Id == originAddress.StateProvinceId);
                request.CountryFrom = country;
                request.StateProvinceFrom = state;
                request.ZipPostalCodeFrom = originAddress.ZipPostalCode;
                request.CityFrom = originAddress.City;
                request.AddressFrom = originAddress.Address1;
            }
            foreach (var sci in cart)
            {
                if (!sci.IsShipEnabled)
                    continue;
                //add item
                request.Items.Add(new GetShippingOptionRequest.PackageItem(sci));
            }

            return request;
        }

        /// <summary>
        ///  Gets available shipping options
        /// </summary>
        /// <param name="customer"></param>
        /// <param name="cart">Shopping cart</param>
        /// <param name="shippingAddress">Shipping address</param>
        /// <param name="allowedShippingRateMethodSystemName">Filter by Shipping rate method identifier; null to load shipping options of all Shipping rate  methods</param>
        /// <param name="store">Store</param>
        /// <returns>Shipping options</returns>
        public virtual async Task<GetShippingOptionResponse> GetShippingOptions(Customer customer, IList<ShoppingCartItem> cart,
            Address shippingAddress, string allowedShippingRateMethodSystemName = "",
            Store store = null)
        {
            if (cart == null)
                throw new ArgumentNullException(nameof(cart));

            var result = new GetShippingOptionResponse();

            //create a package
            var shippingOptionRequest = await CreateShippingOptionRequests(customer, cart, shippingAddress, store);

            var shippingRateMethods = await LoadActiveShippingRateCalculationProviders(customer, store?.Id, cart);
            //filter by system name
            if (!string.IsNullOrWhiteSpace(allowedShippingRateMethodSystemName))
            {
                shippingRateMethods = shippingRateMethods
                    .Where(srcm => allowedShippingRateMethodSystemName.Equals(srcm.SystemName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            if (!shippingRateMethods.Any())
                throw new GrandException("Shipping rate  method could not be loaded");

            //request shipping options from each Shipping rate  methods
            foreach (var shippingRateMethod in shippingRateMethods)
            {
                //request shipping options (separately for each package-request)
                IList<ShippingOption> shippingRateMethodOptions = null;
                var getShippingOptionResponse = await shippingRateMethod.GetShippingOptions(shippingOptionRequest);

                if (getShippingOptionResponse.Success)
                {
                    //success
                    shippingRateMethodOptions = getShippingOptionResponse.ShippingOptions;
                }
                else
                {
                    //errors
                    foreach (var error in getShippingOptionResponse.Errors)
                    {
                        result.AddError(error);
                        _ = _logger.Warning($"Shipping ({shippingRateMethod.FriendlyName}). {error}");
                    }
                    //clear the shipping options in this case
                    break;
                }
                if (shippingRateMethodOptions == null) continue;
                foreach (var so in shippingRateMethodOptions)
                {
                    //set system name if not set yet
                    if (string.IsNullOrEmpty(so.ShippingRateProviderSystemName))
                        so.ShippingRateProviderSystemName = shippingRateMethod.SystemName;

                    result.ShippingOptions.Add(so);
                }
            }

            //return valid options if there are any (no matter of the errors returned by other shipping rate computation methods).
            if (!result.ShippingOptions.Any() && !result.Errors.Any())
                result.Errors.Clear();

            //no shipping options loaded
            if (result.ShippingOptions.Count == 0 && result.Errors.Count == 0)
                result.Errors.Add(_translationService.GetResource("Checkout.ShippingOptionCouldNotBeLoaded"));

            return result;
        }

        #endregion
    }
}