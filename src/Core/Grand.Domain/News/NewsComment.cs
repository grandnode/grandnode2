using Grand.Domain.Stores;
using Grand.Domain.Customers;
using Grand.Domain.Audit;

namespace Grand.Domain.News;

/// <summary>
///     Represents a news comment
/// </summary>
public class NewsComment : SubBaseEntity
{
    /// <summary>
    ///     Gets or sets the comment title
    /// </summary>
    [AuditName]
    public string CommentTitle { get; set; }

    /// <summary>
    ///     Gets or sets the comment text
    /// </summary>
    public string CommentText { get; set; }

    /// <summary>
    ///     Gets or sets the news item identifier
    /// </summary>
    [AuditReference(typeof(NewsItem))]
    public string NewsItemId { get; set; }

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
    ///     Gets or sets the date and time of instance creation
    /// </summary>
    public DateTime CreatedOnUtc { get; set; }
}