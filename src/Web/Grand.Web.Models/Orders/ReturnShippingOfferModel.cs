namespace Grand.Web.Models.Orders;

/// <summary>
///     A way of sending the items of a merchandise return back the customer can choose
/// </summary>
public class ReturnShippingOfferModel
{
    public string ProviderSystemName { get; set; }
    public string OptionCode { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    /// <summary>
    ///     What the customer pays for all parcels, formatted in the order's currency; "Free" when nothing
    /// </summary>
    public string Cost { get; set; }

    public int Parcels { get; set; }

    /// <summary>
    ///     "× N parcels" when the items go back in more than one parcel, else empty
    /// </summary>
    public string ParcelsText { get; set; }

    /// <summary>
    ///     The address of the option's form, loaded under the option once it is chosen; empty when it has none
    /// </summary>
    public string FormUrl { get; set; }
}
