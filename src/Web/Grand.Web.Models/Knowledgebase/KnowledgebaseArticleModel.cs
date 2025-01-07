using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Knowledgebase;

public class KnowledgebaseArticleModel : BaseEntityModel
{
    public string ArticleId { get; set; }
    public string Name { get; set; }

    public string Content { get; set; }

    public string ParentCategoryId { get; set; }

    public string SeName { get; set; }

    public string MetaKeywords { get; set; }

    public string MetaDescription { get; set; }

    public string MetaTitle { get; set; }

    public bool AllowComments { get; set; }

    public ICaptchaValidModel Captcha { get; set; } = new CaptchaModel();

    public IList<KnowledgebaseArticleModel> RelatedArticles { get; set; } = new List<KnowledgebaseArticleModel>();

    public List<KnowledgebaseCategoryModel> CategoryBreadcrumb { get; set; } = new();

    public AddKnowledgebaseArticleCommentModel AddNewComment { get; set; } = new();

    public IList<KnowledgebaseArticleCommentModel> Comments { get; set; } =
        new List<KnowledgebaseArticleCommentModel>();
}