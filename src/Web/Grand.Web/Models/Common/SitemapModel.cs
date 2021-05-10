using Grand.Infrastructure.Models;
using Grand.Web.Models.Catalog;
using Grand.Web.Models.Pages;
using Grand.Web.Models.Blogs;
using Grand.Web.Models.Knowledgebase;
using System.Collections.Generic;

namespace Grand.Web.Models.Common
{
    public partial class SitemapModel : BaseModel
    {
        public SitemapModel()
        {
            Products = new List<ProductOverviewModel>();
            Categories = new List<CategoryModel>();
            Brands = new List<BrandModel>();
            Pages = new List<PageModel>();
            BlogPosts = new List<BlogPostModel>();
            KnowledgebaseArticles = new List<KnowledgebaseItemModel>();
        }
        public IList<ProductOverviewModel> Products { get; set; }
        public IList<CategoryModel> Categories { get; set; }
        public IList<BrandModel> Brands { get; set; }
        public IList<PageModel> Pages { get; set; }
        public IList<BlogPostModel> BlogPosts { get; set; }
        public IList<KnowledgebaseItemModel> KnowledgebaseArticles { get; set; }

        public bool NewsEnabled { get; set; }
        public bool BlogEnabled { get; set; }
        public bool KnowledgebaseEnabled { get; set; }
    }
}