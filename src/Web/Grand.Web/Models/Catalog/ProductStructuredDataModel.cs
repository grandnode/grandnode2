namespace Grand.Web.Models.Catalog;

/// <summary>
///     Facts for the product JSON-LD that the rest of ProductDetailsModel does not carry.
///     Each one is set only when the store's own data makes it true - an unset value means
///     "say nothing", never "false".
/// </summary>
public class ProductStructuredDataModel
{
    /// <summary>
    ///     Average of the approved reviews; null when reviews are off or none is approved
    /// </summary>
    public double? RatingValue { get; set; }

    public int ReviewCount { get; set; }

    /// <summary>
    ///     Cost of shipping this product on its own, in the offer's currency: 0 for free shipping,
    ///     otherwise the store's declared default rate; null when neither applies
    /// </summary>
    public double? ShippingRate { get; set; }

    /// <summary>
    ///     Business days from order to handover to the carrier, as the store declares them
    ///     (ShippingSettings); null when the product does not ship or nothing usable is declared
    /// </summary>
    public DayRangeModel HandlingTime { get; set; }

    /// <summary>
    ///     Business days the carrier needs, as the store declares them (ShippingSettings)
    /// </summary>
    public DayRangeModel TransitTime { get; set; }

    /// <summary>
    ///     Merchandise returns are on for the store, but this product is excluded from them -
    ///     the offer has to override the Organization-level return policy
    /// </summary>
    public bool ReturnsNotPermitted { get; set; }

    /// <summary>
    ///     ISO 3166-1 alpha-2 codes of the countries the store ships to; filled only when
    ///     shipping details or ReturnsNotPermitted need them
    /// </summary>
    public IList<string> CountryCodes { get; set; } = new List<string>();

    public bool HasShippingDetails => ShippingRate != null || HandlingTime != null || TransitTime != null;

    public class DayRangeModel
    {
        public int? Min { get; set; }
        public int? Max { get; set; }
    }
}
