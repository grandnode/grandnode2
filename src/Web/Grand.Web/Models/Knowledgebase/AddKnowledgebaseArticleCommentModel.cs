using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Knowledgebase
{
    public partial class AddKnowledgebaseArticleCommentModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Knowledgebase.Article.CommentText")]
        public string CommentText { get; set; }

        public bool DisplayCaptcha { get; set; }
    }
}
