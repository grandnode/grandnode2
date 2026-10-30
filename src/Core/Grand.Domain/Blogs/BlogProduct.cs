using Grand.Domain.Audit;

namespace Grand.Domain.Blogs;

public class BlogProduct : BaseEntity
{
    [AuditReference(typeof(BlogPost))]
    public string BlogPostId { get; set; }
    public string ProductId { get; set; }
    public int DisplayOrder { get; set; }
}