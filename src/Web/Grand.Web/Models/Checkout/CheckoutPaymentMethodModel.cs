﻿using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Checkout
{
    public class CheckoutPaymentMethodModel : BaseModel
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
        public string PaymentMethod{ get; set; }
        #region Nested classes

        public class PaymentMethodModel : BaseModel
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