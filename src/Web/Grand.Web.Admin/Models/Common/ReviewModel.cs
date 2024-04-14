using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Common;

public class ReviewModel : BaseEntityModel
{
    /// <summary>
    ///     Gets or sets the title
    /// </summary>
    [GrandResourceDisplayName("Admin.Customers.Customers.Reviews.Title")]
    public string Title { get; set; }

    /// <summary>
    ///     Gets or sets the review text
    /// </summary>
    [GrandResourceDisplayName("Admin.Customers.Customers.Reviews.Review")]
    public string ReviewText { get; set; }
}