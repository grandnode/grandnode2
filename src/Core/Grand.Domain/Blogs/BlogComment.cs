using Grand.Domain.Stores;
using Grand.Domain.Customers;
using Grand.Domain.Audit;

namespace Grand.Domain.Blogs;

/// <summary>
///     Represents a blog comment
/// </summary>
public class BlogComment : BaseEntity
{
    /// <summary>
    ///     Gets or sets the customer identifier
    /// </summary>
    [AuditReference(typeof(Customer))]
    public string CustomerId { get; set; }

    /// <summary>
    ///     Gets or sets the store identifier
    /// </summary>
    [AuditReference(typeof(Store))]
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
    [AuditReference(typeof(BlogPost))]
    public string BlogPostId { get; set; }
}