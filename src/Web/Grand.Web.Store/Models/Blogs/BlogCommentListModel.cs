namespace Grand.Web.Store.Models.Blogs;

/// <summary>
/// View model for the blog comments list page, optionally filtered to one blog post.
/// </summary>
public class BlogCommentListModel
{
    public string FilterByBlogPostId { get; set; }
}
