using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Knowledgebase
{
    public class KnowledgebaseArticleGridModel : BaseEntityModel
    {
        public string Name { get; set; }
        public bool Published { get; set; }
        public int DisplayOrder { get; set; }
        public string ArticleId { get; set; }
    }
}
