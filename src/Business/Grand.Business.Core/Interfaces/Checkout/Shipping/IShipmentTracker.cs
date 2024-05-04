using Grand.Business.Core.Enums.Checkout;

namespace Grand.Business.Core.Interfaces.Checkout.Shipping;

/// <summary>
///     Shipment tracker
/// </summary>
public interface IShipmentTracker
{
    /// <summary>
    ///     Gets a url for a page to show tracking info (third party tracking page).
    /// </summary>
    /// <param name="trackingNumber">The tracking number to track.</param>
    /// <returns>A url to a tracking page.</returns>
    Task<string> GetUrl(string trackingNumber);

    /// <summary>
    ///     Gets all events for a tracking number.
    /// </summary>
    /// <param name="trackingNumber">The tracking number to track</param>
    /// <returns>List of Shipment Events.</returns>
    Task<IList<ShipmentStatusEvent>> GetShipmentEvents(string trackingNumber);
}