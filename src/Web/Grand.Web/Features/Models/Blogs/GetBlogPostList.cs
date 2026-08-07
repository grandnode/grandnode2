using Grand.Web.Models.Blogs;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Blogs;

public class GetBlogPostList : IRequest<BlogPostListModel>
{
    public BlogPagingFilteringModel Command { get; set; } = new();
}