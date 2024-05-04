using Grand.Domain.Localization;
using Grand.Domain.Permissions;
using Grand.Domain.Seo;
using Grand.Domain.Stores;

namespace Grand.Domain.Knowledgebase;

public class KnowledgebaseCategory : BaseEntity, ITreeNode, ITranslationEntity, ISlugEntity, IGroupLinkEntity,
    IStoreLinkEntity
{
    /// <summary>
    ///     Gets or sets display order
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    ///     Gets or sets description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    ///     Gets or sets published
    /// </summary>
    public bool Published { get; set; }

    /// <summary>
    ///     Gets or sets meta keywords
    /// </summary>
    public string MetaKeywords { get; set; }

    /// <summary>
    ///     Gets or sets meta description
    /// </summary>
    public string MetaDescription { get; set; }

    /// <summary>
    ///     Gets or sets meta title
    /// </summary>
    public string MetaTitle { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the entity is subject to ACL
    /// </summary>
    public bool LimitedToGroups { get; set; }

    public IList<string> CustomerGroups { get; set; } = new List<string>();

    /// <summary>
    ///     Gets or sets the name
    /// </summary>
    public string SeName { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the entity is limited/restricted to certain stores
    /// </summary>
    public bool LimitedToStores { get; set; }

    public IList<string> Stores { get; set; } = new List<string>();

    /// <summary>
    ///     Gets or sets the collection of locales
    /// </summary>
    public IList<TranslationEntity> Locales { get; set; } = new List<TranslationEntity>();

    /// <summary>
    ///     Gets or sets name of the category
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     Gets or sets parent category Id
    /// </summary>
    public string ParentCategoryId { get; set; }
}