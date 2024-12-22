using Grand.Infrastructure.Models;
using Grand.Web.Models.Media;

namespace Grand.Web.Models.Blogs;

public class BlogPostListModel : BaseModel
{
    public PictureModel PictureModel { get; set; } = new();
    public string WorkingLanguageId { get; set; }
    public BlogPagingFilteringModel PagingFilteringContext { get; set; } = new();
    public IList<BlogPostModel> BlogPosts { get; set; } = new List<BlogPostModel>();
    public string SearchKeyword { get; set; }
}