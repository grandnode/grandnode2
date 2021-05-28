using Grand.Business.Checkout.Enum;
using Grand.Business.Checkout.Utilities;
using Grand.Infrastructure.Plugins;
using Grand.Domain.Orders;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Interfaces.Shipping
{
    /// <summary>
    /// Provides an interface of Shipping rate  method
    /// </summary>
    public partial interface IShippingRateCalculationProvider : IProvider
    {
        /// <summary>
        /// Gets a Shipping rate  method type
        /// </summary>
        ShippingRateCalculationType ShippingRateCalculationType { get; }

        /// <summary>
        ///  Gets available shipping options
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Represents a response of getting shipping rate options</returns>
        Task<GetShippingOptionResponse> GetShippingOptions(GetShippingOptionRequest getShippingOptionRequest);

        /// <summary>
        /// Returns a value indicating whether shipping methods should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        Task<bool> HideShipmentMethods(IList<ShoppingCartItem> cart);

        /// <summary>
        /// Gets fixed shipping rate (if Shipping rate  method allows it and the rate can be calculated before checkout).
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Fixed shipping rate; or null in case there's no fixed shipping rate</returns>
        Task<double?> GetFixedRate(GetShippingOptionRequest getShippingOptionRequest);

        /// <summary>
        /// Gets a shipment tracker
        /// </summary>
        IShipmentTracker ShipmentTracker { get; }


        Task<IList<string>> ValidateShippingForm(IFormCollection form);

        /// <summary>
        /// Gets a view component for displaying plugin ("shipping" checkout step)
        /// </summary>
        /// <param name="viewComponentName">View component name</param>
        Task<string> GetPublicViewComponentName();

    }
}
