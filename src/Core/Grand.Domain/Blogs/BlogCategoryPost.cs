using Grand.Domain.Audit;

namespace Grand.Domain.Blogs;

public class BlogCategoryPost : SubBaseEntity
{
    [AuditReference(typeof(BlogPost))]
    public string BlogPostId { get; set; }
}