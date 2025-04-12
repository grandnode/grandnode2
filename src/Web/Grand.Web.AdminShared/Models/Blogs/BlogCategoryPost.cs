using Grand.Infrastructure.Models;

namespace Grand.Web.AdminShared.Models.Blogs;

public class BlogCategoryPost : BaseEntityModel
{
    public string BlogPostId { get; set; }
    public string Name { get; set; }
}