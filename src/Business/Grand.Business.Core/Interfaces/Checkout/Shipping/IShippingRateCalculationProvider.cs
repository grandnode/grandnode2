using Grand.Business.Core.Enums.Checkout;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Infrastructure.Plugins;

namespace Grand.Business.Core.Interfaces.Checkout.Shipping;

/// <summary>
///     Provides an interface of Shipping rate  method
/// </summary>
public interface IShippingRateCalculationProvider : IProvider
{
    /// <summary>
    ///     Gets a Shipping rate  method type
    /// </summary>
    ShippingRateCalculationType ShippingRateCalculationType { get; }

    /// <summary>
    ///     Gets a shipment tracker
    /// </summary>
    IShipmentTracker ShipmentTracker { get; }

    /// <summary>
    ///     Gets available shipping options
    /// </summary>
    /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
    /// <returns>Represents a response of getting shipping rate options</returns>
    Task<GetShippingOptionResponse> GetShippingOptions(GetShippingOptionRequest getShippingOptionRequest);

    /// <summary>
    ///     Returns a value indicating whether shipping methods should be hidden during checkout
    /// </summary>
    /// <param name="cart">Shopping cart</param>
    /// <returns>true - hide; false - display.</returns>
    Task<bool> HideShipmentMethods(IList<ShoppingCartItem> cart);

    /// <summary>
    ///     Gets fixed shipping rate (if Shipping rate  method allows it and the rate can be calculated before checkout).
    /// </summary>
    /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
    /// <returns>Fixed shipping rate; or null in case there's no fixed shipping rate</returns>
    Task<double?> GetFixedRate(GetShippingOptionRequest getShippingOptionRequest);

    Task<IList<string>> ValidateShippingForm(string shippingOption, IDictionary<string, string> data);

    /// <summary>
    ///     Gets a route name for displaying plugin ("shipping" checkout step)
    /// </summary>
    Task<string> GetControllerRouteName();
}