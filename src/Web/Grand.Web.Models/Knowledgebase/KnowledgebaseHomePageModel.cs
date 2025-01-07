using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Knowledgebase;

public class KnowledgebaseHomePageModel : BaseEntityModel
{
    public List<KnowledgebaseItemModel> Items { get; set; } = new();
    public List<KnowledgebaseCategoryModel> Categories { get; set; } = new();
    public string CurrentCategoryId { get; set; }
    public string CurrentCategoryDescription { get; set; }
    public string CurrentCategoryMetaTitle { get; set; }
    public string CurrentCategoryMetaDescription { get; set; }
    public string CurrentCategoryMetaKeywords { get; set; }
    public string CurrentCategoryName { get; set; }
    public string CurrentCategorySeName { get; set; }
    public List<KnowledgebaseCategoryModel> CategoryBreadcrumb { get; set; } = new();
    public string SearchKeyword { get; set; }
}