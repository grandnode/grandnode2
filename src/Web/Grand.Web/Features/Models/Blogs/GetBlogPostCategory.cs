using Grand.Web.Models.Blogs;
using MediatR;

namespace Grand.Web.Features.Models.Blogs
{
    public class GetBlogPostCategory : IRequest<IList<BlogPostCategoryModel>>
    {
    }
}
