using Grand.Infrastructure.Models;
using Grand.Web.Models.Blogs;
using Grand.Web.Models.Catalog;
using Grand.Web.Models.Knowledgebase;
using Grand.Web.Models.Pages;

namespace Grand.Web.Models.Common;

public class SitemapModel : BaseModel
{
    public IList<ProductOverviewModel> Products { get; set; } = new List<ProductOverviewModel>();
    public IList<CategoryModel> Categories { get; set; } = new List<CategoryModel>();
    public IList<BrandModel> Brands { get; set; } = new List<BrandModel>();
    public IList<PageModel> Pages { get; set; } = new List<PageModel>();
    public IList<BlogPostModel> BlogPosts { get; set; } = new List<BlogPostModel>();
    public IList<KnowledgebaseItemModel> KnowledgebaseArticles { get; set; } = new List<KnowledgebaseItemModel>();

    public bool NewsEnabled { get; set; }
    public bool BlogEnabled { get; set; }
    public bool KnowledgebaseEnabled { get; set; }
}