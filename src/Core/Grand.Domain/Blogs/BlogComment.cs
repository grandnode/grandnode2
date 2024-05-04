namespace Grand.Domain.Blogs;

/// <summary>
///     Represents a blog comment
/// </summary>
public class BlogComment : BaseEntity
{
    /// <summary>
    ///     Gets or sets the customer identifier
    /// </summary>
    public string CustomerId { get; set; }

    /// <summary>
    ///     Gets or sets the store identifier
    /// </summary>
    public string StoreId { get; set; }

    /// <summary>
    ///     Gets or sets the comment text
    /// </summary>
    public string CommentText { get; set; }

    /// <summary>
    ///     Gets or sets the blog post title
    /// </summary>
    public string BlogPostTitle { get; set; }

    /// <summary>
    ///     Gets or sets the blog post identifier
    /// </summary>
    public string BlogPostId { get; set; }
}