using Grand.Domain.Common;
using Grand.Domain.Orders;

namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     What a customer asks to return of an order
/// </summary>
public class MerchandiseReturnSubmitRequest
{
    public Order Order { get; set; }
    public string StoreId { get; set; }
    public string CustomerId { get; set; }
    public string OwnerId { get; set; }

    /// <summary>
    ///     The language the reason and action names are kept in, as the customer read them
    /// </summary>
    public string LanguageId { get; set; }

    public string Comments { get; set; }
    public Address PickupAddress { get; set; }
    public DateTime? PickupDate { get; set; }
    public IList<MerchandiseReturnSubmitItem> Items { get; set; } = [];

    /// <summary>
    ///     How the customer sends the items back; null when no return shipping option was picked
    /// </summary>
    public ReturnShippingChoice ReturnShipping { get; set; }
}

/// <summary>
///     One order item of a merchandise return request
/// </summary>
public record MerchandiseReturnSubmitItem(string OrderItemId, int Quantity, string ReasonId, string ActionId);

/// <summary>
///     The merchandise returns created, one for each vendor of the items; none, and why, when Error is set or
///     ShippingErrors has any
/// </summary>
public class MerchandiseReturnSubmitResult
{
    public IList<MerchandiseReturn> MerchandiseReturns { get; } = [];
    public string Error { get; set; }

    /// <summary>
    ///     Why the return shipping option picked cannot be used: keys of MerchandiseReturnErrors, or texts a provider
    ///     translated already
    /// </summary>
    public IList<string> ShippingErrors { get; } = [];
}
