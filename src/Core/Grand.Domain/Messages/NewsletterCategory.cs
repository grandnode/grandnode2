using Grand.Domain.Localization;
using Grand.Domain.Stores;

namespace Grand.Domain.Messages;

public class NewsletterCategory : BaseEntity, ITranslationEntity, IStoreLinkEntity
{
    /// <summary>
    ///     Gets or sets the name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     Gets or sets the description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    ///     Gets or sets the selected
    /// </summary>
    public bool Selected { get; set; }

    public int DisplayOrder { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the entity is limited/restricted to certain stores
    /// </summary>
    public bool LimitedToStores { get; set; }

    public IList<string> Stores { get; set; } = new List<string>();

    public IList<TranslationEntity> Locales { get; set; } = new List<TranslationEntity>();
}