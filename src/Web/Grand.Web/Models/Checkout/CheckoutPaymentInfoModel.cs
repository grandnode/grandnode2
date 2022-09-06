using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Checkout
{
    public partial class CheckoutPaymentInfoModel : BaseModel
    {
        public string PaymentUrl { get; set; }       
    }
}