using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Infrastructure.Plugins;

namespace Grand.Business.Core.Interfaces.Checkout.Shipping;

/// <summary>
///     Provides the ways a customer sends the items of a merchandise return back
/// </summary>
public interface IReturnShippingProvider : IProvider
{
    /// <summary>
    ///     Gets the options for the items of one vendor of a return
    /// </summary>
    Task<IList<ReturnShippingOption>> GetReturnOptions(ReturnShippingRequest request);

    /// <summary>
    ///     Gets the route name of the form of an option that has one
    /// </summary>
    Task<string> GetFormRouteName(string optionCode);

    /// <summary>
    ///     Validates the form the customer filled in for an option, for the items of one vendor of a return
    /// </summary>
    Task<ReturnShippingValidationResult> ValidateForm(ReturnShippingRequest request, string optionCode,
        IDictionary<string, string> form);

    /// <summary>
    ///     Creates the label, or only the instructions, of a saved merchandise return
    /// </summary>
    Task<ReturnLabelResult> CreateLabel(MerchandiseReturn merchandiseReturn);

    /// <summary>
    ///     Cancels the label of a merchandise return
    /// </summary>
    Task<bool> CancelLabel(MerchandiseReturn merchandiseReturn);

    /// <summary>
    ///     Gets a shipment tracker; null when the provider tracks nothing
    /// </summary>
    IShipmentTracker ShipmentTracker { get; }
}
