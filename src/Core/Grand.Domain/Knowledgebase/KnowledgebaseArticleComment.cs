using Grand.Domain.Customers;
using Grand.Domain.Audit;

namespace Grand.Domain.Knowledgebase;

public class KnowledgebaseArticleComment : BaseEntity
{
    /// <summary>
    ///     Gets or sets the customer identifier
    /// </summary>
    [AuditReference(typeof(Customer))]
    public string CustomerId { get; set; }

    /// <summary>
    ///     Gets or sets the comment text
    /// </summary>
    public string CommentText { get; set; }

    /// <summary>
    ///     Gets or sets the blog post title
    /// </summary>
    public string ArticleTitle { get; set; }

    /// <summary>
    ///     Gets or sets the blog post identifier
    /// </summary>
    [AuditReference(typeof(KnowledgebaseArticle))]
    public string ArticleId { get; set; }
}