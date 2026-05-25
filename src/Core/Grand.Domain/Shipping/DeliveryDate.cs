using Grand.Domain.Localization;
using Grand.Domain.Stores;

namespace Grand.Domain.Shipping;

/// <summary>
///     Represents a delivery date
/// </summary>
public class DeliveryDate : BaseEntity, ITranslationEntity, IStoreLinkEntity
{
    /// <summary>
    ///     Gets or sets the name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     Gets or sets the display order
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    ///     Gets or sets the color RGB value (used with "Color squares" attribute type)
    /// </summary>
    public string ColorSquaresRgb { get; set; }

    /// <summary>
    ///     Gets or sets the collection of locales
    /// </summary>
    public IList<TranslationEntity> Locales { get; set; } = new List<TranslationEntity>();

    /// <summary>
    ///     Gets or sets a value indicating whether the entity is limited to certain stores
    /// </summary>
    public bool LimitedToStores { get; set; }

    /// <summary>
    ///     Gets or sets the stores the delivery date is limited to
    /// </summary>
    public IList<string> Stores { get; set; } = new List<string>();
}