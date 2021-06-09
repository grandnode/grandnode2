using Grand.Business.Checkout.Utilities;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Interfaces.Shipping
{
    /// <summary>
    /// Shipping service interface
    /// </summary>
    public partial interface IShippingService
    {
        /// <summary>
        /// Load active Shipping rate  providers
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="storeId">Store ident</param>
        /// <returns>Shipping rate  methods</returns>
        Task<IList<IShippingRateCalculationProvider>> LoadActiveShippingRateCalculationProviders(Customer customer = null, string storeId = "", IList<ShoppingCartItem> cart = null);

        /// <summary>
        /// Load Shipping rate  provider by system name
        /// </summary>
        /// <param name="systemName">System name</param>
        /// <returns>Found Shipping rate  provider</returns>
        IShippingRateCalculationProvider LoadShippingRateCalculationProviderBySystemName(string systemName);

        /// <summary>
        /// Load all Shipping rate  providers
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="storeId">Store ident</param>
        /// <returns>Shipping rate  providers</returns>
        IList<IShippingRateCalculationProvider> LoadAllShippingRateCalculationProviders(Customer customer = null, string storeId = "");

        /// <summary>
        /// Create shipment packages (requests) from shopping cart
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="cart">Shopping cart</param>
        /// <param name="shippingAddress">Shipping address</param>
        /// <param name="store">Store</param>
        /// <returns>Shipment packages</returns>
        Task<GetShippingOptionRequest> CreateShippingOptionRequests(Customer customer,
            IList<ShoppingCartItem> cart, Address shippingAddress, Store store);

        /// <summary>
        ///  Gets available shipping options
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <param name="shippingAddress">Shipping address</param>
        /// <param name="allowedShippingRateMethodSystemName">Filter by Shipping rate  method identifier; null to load shipping options of all Shipping rate  methods</param>
        /// <param name="storeId">Load records allowed only in a specified store; pass "" to load all records</param>
        /// <returns>Shipping options</returns>
        Task<GetShippingOptionResponse> GetShippingOptions(Customer customer, IList<ShoppingCartItem> cart, Address shippingAddress,
            string allowedShippingRateMethodSystemName = "", Store store = null);
    }
}
