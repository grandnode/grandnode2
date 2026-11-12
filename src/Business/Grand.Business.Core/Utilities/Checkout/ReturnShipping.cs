using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;

namespace Grand.Business.Core.Utilities.Checkout;

/// <summary>
///     The items of one vendor of a merchandise return, which a return shipping provider offers its options for
/// </summary>
public class ReturnShippingRequest
{
    public Order Order { get; set; }
    public Customer Customer { get; set; }
    public string StoreId { get; set; }

    /// <summary>
    ///     The vendor the items are sent back to; empty for the store
    /// </summary>
    public string VendorId { get; set; }

    public IList<(OrderItem OrderItem, int Quantity)> Items { get; set; } = [];

    /// <summary>
    ///     The address the parcel is sent to
    /// </summary>
    public Address ReturnAddress { get; set; }

    public Address PickupAddress { get; set; }
    public string LanguageId { get; set; }
}

/// <summary>
///     One way of sending the items back a provider offers
/// </summary>
public class ReturnShippingOption
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    /// <summary>
    ///     The cost of the option, in the primary store currency, before the store's return fee rules
    /// </summary>
    public double Cost { get; set; }

    /// <summary>
    ///     Whether the cost depends on what is chosen in the option's form (e.g. the drop-off point); Cost is then
    ///     the lowest one
    /// </summary>
    public bool CostVaries { get; set; }

    /// <summary>
    ///     Whether the customer fills in a form of the provider, e.g. to pick a drop-off point
    /// </summary>
    public bool HasForm { get; set; }

    /// <summary>
    ///     Whether a label or code is created with the provider once the return is saved; false when the provider only
    ///     gives instructions
    /// </summary>
    public bool CreatesLabel { get; set; }
}

/// <summary>
///     What a provider made of the form the customer filled in; the return is not saved when Errors has any
/// </summary>
public class ReturnShippingValidationResult
{
    /// <summary>
    ///     Errors, already translated, shown to the customer as they are
    /// </summary>
    public IList<string> Errors { get; set; } = [];

    /// <summary>
    ///     The cost for what was chosen in the form; null keeps the cost of the option
    /// </summary>
    public double? Cost { get; set; }

    public ReturnDropOffPoint DropOffPoint { get; set; }
    public Dictionary<string, string> ProviderData { get; set; } = new();
}

/// <summary>
///     The label or code a provider created for a merchandise return
/// </summary>
public class ReturnLabelResult
{
    public bool Success { get; set; }
    public string Error { get; set; }
    public string TrackingNumber { get; set; }
    public string ReturnCode { get; set; }
    public ReturnLabelFile LabelFile { get; set; }
    public string Instructions { get; set; }
    public Dictionary<string, string> ProviderData { get; set; } = new();
}

/// <summary>
///     The label file the customer downloads
/// </summary>
public record ReturnLabelFile(byte[] Content, string ContentType, string FileName);

/// <summary>
///     The return shipping option the customer picked
/// </summary>
public class ReturnShippingChoice
{
    public string ProviderSystemName { get; set; }
    public string OptionCode { get; set; }

    /// <summary>
    ///     The fields posted under the "returnshipping." prefix, prefix stripped
    /// </summary>
    public IDictionary<string, string> Form { get; set; } = new Dictionary<string, string>();
}

/// <summary>
///     One option the customer can pick to send the items of a return back, offered for every vendor of the items
/// </summary>
public class ReturnShippingOffer
{
    public string ProviderSystemName { get; set; }
    public string OptionCode { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    /// <summary>
    ///     What the customer pays for all parcels, in the primary store currency
    /// </summary>
    public double Cost { get; set; }

    /// <summary>
    ///     Whether what the customer pays depends on what is chosen in the option's form; Cost is then the lowest.
    ///     False when the store's return fee rules make the return free whatever is chosen
    /// </summary>
    public bool CostVaries { get; set; }

    /// <summary>
    ///     The number of parcels: one for each vendor of the items
    /// </summary>
    public int Parcels { get; set; }

    public bool HasForm { get; set; }
    public string FormRouteName { get; set; }
}

/// <summary>
///     The return shipping of each vendor of a return; none, and why, when Errors has any
/// </summary>
public class ReturnShippingResolution
{
    public IList<string> Errors { get; set; } = [];

    /// <summary>
    ///     The return shipping by VendorId ("" for the store), the keys of the returns a submit creates
    /// </summary>
    public IDictionary<string, MerchandiseReturnShipping> ByVendor { get; set; } =
        new Dictionary<string, MerchandiseReturnShipping>();
}
