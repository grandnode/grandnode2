using Grand.Domain.Localization;

namespace Grand.Domain.Orders;

/// <summary>
///     Represents a merchandise return action
/// </summary>
public class MerchandiseReturnAction : BaseEntity, ITranslationEntity
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
    ///     Gets or sets the collection of locales
    /// </summary>
    public IList<TranslationEntity> Locales { get; set; } = new List<TranslationEntity>();
}