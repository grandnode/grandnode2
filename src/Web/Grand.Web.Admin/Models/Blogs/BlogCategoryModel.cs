using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Link;
using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Blogs
{
    public partial class BlogCategoryModel : BaseEntityModel, ILocalizedModel<BlogCategoryLocalizedModel>, IStoreLinkModel
    {
        public BlogCategoryModel()
        {
            Locales = new List<BlogCategoryLocalizedModel>();
        }
        [GrandResourceDisplayName("Admin.Content.Blog.BlogCategory.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Content.Blog.BlogCategory.Fields.SeName")]
        public string SeName { get; set; }

        [GrandResourceDisplayName("Admin.Content.Blog.BlogCategory.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public IList<BlogCategoryLocalizedModel> Locales { get; set; }
        //Store acl
        [UIHint("Stores")]
        public string[] Stores { get; set; }
    }
    public partial class BlogCategoryLocalizedModel : ILocalizedModelLocal
    {
        public string LanguageId { get; set; }
        [GrandResourceDisplayName("Admin.Content.Blog.BlogCategory.Fields.Name")]
        public string Name { get; set; }
    }

    public partial class AddBlogPostCategoryModel : BaseModel
    {
        public AddBlogPostCategoryModel()
        {
            AvailableStores = new List<SelectListItem>();
        }

        [GrandResourceDisplayName("Admin.Content.Blog.BlogCategory.SearchBlogTitle")]

        public string SearchBlogTitle { get; set; }
        [GrandResourceDisplayName("Admin.Content.Blog.BlogCategory.SearchStore")]
        public string SearchStoreId { get; set; }

        public IList<SelectListItem> AvailableStores { get; set; }

        public string CategoryId { get; set; }

        public string[] SelectedBlogPostIds { get; set; }
    }
}
