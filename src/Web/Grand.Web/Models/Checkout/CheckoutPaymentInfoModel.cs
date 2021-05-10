using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Checkout
{
    public partial class CheckoutPaymentInfoModel : BaseModel
    {
        public string PaymentViewComponentName { get; set; }       
    }
}