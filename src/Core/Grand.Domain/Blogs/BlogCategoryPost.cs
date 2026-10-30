using Grand.Domain.Audit;

namespace Grand.Domain.Blogs;

public class BlogCategoryPost : SubBaseEntity
{
    [AuditReference(typeof(BlogPost))]
    [AuditName]
    public string BlogPostId { get; set; }
}