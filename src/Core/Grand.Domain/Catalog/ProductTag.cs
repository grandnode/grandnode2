using Grand.Domain.Localization;

namespace Grand.Domain.Catalog;

/// <summary>
///     Represents a product tag
/// </summary>
public class ProductTag : BaseEntity, ITranslationEntity
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
    ///     Gets or sets the count
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    ///     Gets or sets the collection of locales
    /// </summary>
    public IList<TranslationEntity> Locales { get; set; } = new List<TranslationEntity>();
}