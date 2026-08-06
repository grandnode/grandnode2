using Grand.Domain.Blogs;
using Grand.Web.Models.Blogs;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Blogs;

public class GetBlogPost : IRequest<BlogPostModel>
{
    public BlogPost BlogPost { get; set; }
}