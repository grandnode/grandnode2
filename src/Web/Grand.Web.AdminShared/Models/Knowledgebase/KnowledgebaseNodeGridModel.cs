using Grand.Infrastructure.Models;

namespace Grand.Web.AdminShared.Models.Knowledgebase;

/// <summary>
///     A row of the knowledgebase list: a category, which expands to its subcategories and
///     articles, or an article.
/// </summary>
public class KnowledgebaseNodeGridModel : BaseEntityModel
{
    public string Name { get; set; }
    public bool IsCategory { get; set; }

    /// <summary>Subcategories and articles of a category; a row without children has nothing to expand.</summary>
    public int ChildCount { get; set; }

    public bool Published { get; set; }
    public int DisplayOrder { get; set; }
}
