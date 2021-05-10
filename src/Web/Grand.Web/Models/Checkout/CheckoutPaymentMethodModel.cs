using Grand.Infrastructure.Models;
using System.Collections.Generic;

namespace Grand.Web.Models.Checkout
{
    public partial class CheckoutPaymentMethodModel : BaseModel
    {
        public CheckoutPaymentMethodModel()
        {
            PaymentMethods = new List<PaymentMethodModel>();
        }

        public IList<PaymentMethodModel> PaymentMethods { get; set; }

        public bool DisplayLoyaltyPoints { get; set; }
        public int LoyaltyPointsBalance { get; set; }
        public string LoyaltyPointsAmount { get; set; }
        public bool LoyaltyPointsEnoughToPayForOrder { get; set; }
        public bool UseLoyaltyPoints { get; set; }

        #region Nested classes

        public partial class PaymentMethodModel : BaseModel
        {
            public string PaymentMethodSystemName { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Fee { get; set; }
            public bool Selected { get; set; }
            public string LogoUrl { get; set; }
        }
        #endregion
    }
}