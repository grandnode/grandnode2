using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Checkout
{
    public class CheckoutPaymentInfoModel : BaseModel
    {
        public string PaymentUrl { get; set; }
        public string SystemName { get; set; }
    }
}