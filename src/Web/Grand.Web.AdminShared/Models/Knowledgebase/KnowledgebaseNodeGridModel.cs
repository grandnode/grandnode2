using Grand.Infrastructure.Models;

namespace Grand.Web.AdminShared.Models.Knowledgebase;

/// <summary>
///     A row of the knowledgebase list: a category (expandable to its articles) or an
///     article that has no parent category.
/// </summary>
public class KnowledgebaseNodeGridModel : BaseEntityModel
{
    public string Name { get; set; }
    public bool IsCategory { get; set; }

    /// <summary>Breadcrumb of the parent category; empty for a node at the root.</summary>
    public string ParentCategory { get; set; }

    public bool Published { get; set; }
    public int DisplayOrder { get; set; }
}
