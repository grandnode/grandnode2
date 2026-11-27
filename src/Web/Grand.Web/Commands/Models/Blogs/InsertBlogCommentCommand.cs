using Grand.Domain.Blogs;
using Grand.Web.Models.Blogs;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Blogs;

public class InsertBlogCommentCommand : IRequest<BlogComment>
{
    public required AddBlogCommentModel Model { get; set; }
    public required BlogPost BlogPost { get; set; }
}