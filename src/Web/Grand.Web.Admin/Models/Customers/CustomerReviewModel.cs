using Grand.Infrastructure.Models;
using Grand.Web.Admin.Models.Common;

namespace Grand.Web.Admin.Models.Customers
{
    public partial class CustomerReviewModel
    {
        public string CustomerId { get; set; }

        public ReviewModel Review { get; set; }
    }
}