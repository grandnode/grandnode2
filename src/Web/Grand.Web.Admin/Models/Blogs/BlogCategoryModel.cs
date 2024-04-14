using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Link;
using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Blogs;

public class BlogCategoryModel : BaseEntityModel, ILocalizedModel<BlogCategoryLocalizedModel>, IStoreLinkModel
{
    [GrandResourceDisplayName("Admin.Content.Blog.BlogCategory.Fields.Name")]
    public string Name { get; set; }

    [GrandResourceDisplayName("Admin.Content.Blog.BlogCategory.Fields.SeName")]
    public string SeName { get; set; }

    [GrandResourceDisplayName("Admin.Content.Blog.BlogCategory.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    public IList<BlogCategoryLocalizedModel> Locales { get; set; } = new List<BlogCategoryLocalizedModel>();

    //Store acl
    [UIHint("Stores")] public string[] Stores { get; set; }
}

public class BlogCategoryLocalizedModel : ILocalizedModelLocal
{
    [GrandResourceDisplayName("Admin.Content.Blog.BlogCategory.Fields.Name")]
    public string Name { get; set; }

    public string LanguageId { get; set; }
}

public class AddBlogPostCategoryModel : BaseModel
{
    [GrandResourceDisplayName("Admin.Content.Blog.BlogCategory.SearchBlogTitle")]

    public string SearchBlogTitle { get; set; }

    [GrandResourceDisplayName("Admin.Content.Blog.BlogCategory.SearchStore")]
    public string SearchStoreId { get; set; }

    public IList<SelectListItem> AvailableStores { get; set; } = new List<SelectListItem>();

    public string CategoryId { get; set; }

    public string[] SelectedBlogPostIds { get; set; }
}