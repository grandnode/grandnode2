using Grand.Domain.Common;

namespace Grand.Domain.Orders;

/// <summary>
///     Represents how the items of a merchandise return are sent back
/// </summary>
public class MerchandiseReturnShipping
{
    /// <summary>
    ///     Gets or sets the system name of the return shipping provider
    /// </summary>
    public string ProviderSystemName { get; set; }

    /// <summary>
    ///     Gets or sets the code of the chosen option of the provider
    /// </summary>
    public string OptionCode { get; set; }

    /// <summary>
    ///     Gets or sets the name of the chosen option of the provider
    /// </summary>
    public string OptionName { get; set; }

    /// <summary>
    ///     Gets or sets the cost the customer pays, in the primary store currency
    /// </summary>
    public double Cost { get; set; }

    /// <summary>
    ///     Gets or sets the currency rate the cost was calculated with
    /// </summary>
    public double CurrencyRate { get; set; }

    /// <summary>
    ///     Gets or sets the drop-off point the customer leaves the parcel at
    /// </summary>
    public ReturnDropOffPoint DropOffPoint { get; set; }

    /// <summary>
    ///     Gets or sets the address the parcel is sent to
    /// </summary>
    public Address ReturnAddress { get; set; }

    /// <summary>
    ///     Gets or sets the tracking number
    /// </summary>
    public string TrackingNumber { get; set; }

    /// <summary>
    ///     Gets or sets the code the customer shows at the drop-off point
    /// </summary>
    public string ReturnCode { get; set; }

    /// <summary>
    ///     Gets or sets the identifier of the download holding the label
    /// </summary>
    public string LabelDownloadId { get; set; }

    /// <summary>
    ///     Gets or sets the instructions shown to the customer
    /// </summary>
    public string Instructions { get; set; }

    /// <summary>
    ///     Gets or sets the label status identifier
    /// </summary>
    public int LabelStatusId { get; set; }

    /// <summary>
    ///     Gets or sets the label status
    /// </summary>
    public ReturnLabelStatus LabelStatus {
        get => (ReturnLabelStatus)LabelStatusId;
        set => LabelStatusId = (int)value;
    }

    /// <summary>
    ///     Gets or sets the error of the last failed label request
    /// </summary>
    public string LabelError { get; set; }

    /// <summary>
    ///     Gets or sets the shipping status identifier
    /// </summary>
    public int ShippingStatusId { get; set; }

    /// <summary>
    ///     Gets or sets the shipping status
    /// </summary>
    public ReturnShippingStatus ShippingStatus {
        get => (ReturnShippingStatus)ShippingStatusId;
        set => ShippingStatusId = (int)value;
    }

    /// <summary>
    ///     Gets or sets the data only the provider understands
    /// </summary>
    public Dictionary<string, string> ProviderData { get; set; } = new();
}

/// <summary>
///     Represents a place the customer leaves the return parcel at
/// </summary>
public class ReturnDropOffPoint
{
    /// <summary>
    ///     Gets or sets the provider's identifier of the point
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    ///     Gets or sets the name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     Gets or sets the address
    /// </summary>
    public Address Address { get; set; }

    /// <summary>
    ///     Gets or sets the opening hours
    /// </summary>
    public string OpeningHours { get; set; }

    /// <summary>
    ///     Gets or sets the warehouse identifier when the point is a warehouse of the store
    /// </summary>
    public string WarehouseId { get; set; }
}
