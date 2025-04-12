using Grand.Web.AdminShared.Models.Common;

namespace Grand.Web.AdminShared.Models.Customers;

public class CustomerReviewModel
{
    public string CustomerId { get; set; }

    public ReviewModel Review { get; set; }
}