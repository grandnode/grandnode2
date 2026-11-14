using Grand.Domain.Audit;
using Grand.Domain.Localization;
using Grand.Domain.Stores;

namespace Grand.Domain.Orders;

/// <summary>
///     Represents a merchandise return reason
/// </summary>
public class MerchandiseReturnReason : BaseEntity, ITranslationEntity, IStoreLinkEntity
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

    /// <summary>
    ///     Gets or sets a value indicating whether the entity is limited/restricted to certain stores
    /// </summary>
    public bool LimitedToStores { get; set; }

    /// <summary>
    ///     Gets or sets the stores
    /// </summary>
    [AuditReference(typeof(Store))]
    public IList<string> Stores { get; set; } = new List<string>();

    /// <summary>
    ///     Gets or sets a value indicating whether a return for this reason is shipped at no cost to the customer
    /// </summary>
    public bool FreeReturnShipping { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the customer adds a photo of each item returned for this reason
    /// </summary>
    public bool PhotoRequired { get; set; }
}