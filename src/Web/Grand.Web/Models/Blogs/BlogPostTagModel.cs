using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Blogs
{
    public class BlogPostTagModel : BaseModel
    {
        public string Name { get; set; }

        public int BlogPostCount { get; set; }
    }
}