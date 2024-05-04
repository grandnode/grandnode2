using Grand.Domain.Common;

namespace Grand.Domain.Shipping;

/// <summary>
///     Represents a shipment
/// </summary>
public class Warehouse : BaseEntity
{
    /// <summary>
    ///     Gets or sets the warehouse code
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    ///     Gets or sets the warehouse name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     Gets or sets the admin comment
    /// </summary>
    public string AdminComment { get; set; }

    /// <summary>
    ///     Gets or sets the latitude of the GeoCoordinate.
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    ///     Gets or sets the longitude of the GeoCoordinate.
    /// </summary>
    public double? Longitude { get; set; }

    /// <summary>
    ///     Gets or sets the address identifier of the warehouse
    /// </summary>
    public Address Address { get; set; } = new();

    /// <summary>
    ///     Gets or sets the display order
    /// </summary>
    public int DisplayOrder { get; set; }
}