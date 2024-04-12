using Grand.Domain.Localization;
using Grand.Domain.Stores;

namespace Grand.Domain.Catalog;

/// <summary>
///     Represents a product attribute
/// </summary>
public class ProductAttribute : BaseEntity, IStoreLinkEntity, ITranslationEntity
{
    /// <summary>
    ///     Gets or sets the name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     Gets or sets the sename
    /// </summary>
    public string SeName { get; set; }

    /// <summary>
    ///     Gets or sets the description
    /// </summary>
    public string Description { get; set; }

    public IList<PredefinedProductAttributeValue> PredefinedProductAttributeValues { get; set; } =
        new List<PredefinedProductAttributeValue>();

    /// <summary>
    ///     Gets or sets a value indicating whether the entity is limited/restricted to certain stores
    /// </summary>
    public bool LimitedToStores { get; set; }

    public IList<string> Stores { get; set; } = new List<string>();

    /// <summary>
    ///     Gets or sets the collection of locales
    /// </summary>
    public IList<TranslationEntity> Locales { get; set; } = new List<TranslationEntity>();
}