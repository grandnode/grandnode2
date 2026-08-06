using Grand.Web.Models.Blogs;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Blogs;

public class GetBlogPostCategory : IRequest<IList<BlogPostCategoryModel>>;