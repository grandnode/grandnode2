using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Checkout
{
    public partial class CheckoutCompletedModel : BaseModel
    {
        public string OrderId { get; set; }
        public int OrderNumber { get; set; }
        public string OrderCode { get; set; }
    }
}