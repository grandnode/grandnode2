using Grand.Infrastructure.Models;
using System.Collections.Generic;

namespace Grand.Web.Models.ShoppingCart
{
    public partial class OrderTotalsModel : BaseModel
    {
        public OrderTotalsModel()
        {
            TaxRates = new List<TaxRate>();
            GiftVouchers = new List<GiftVoucher>();
        }
        public bool IsEditable { get; set; }

        public string SubTotal { get; set; }

        public string SubTotalDiscount { get; set; }

        public string Shipping { get; set; }
        public bool RequiresShipping { get; set; }
        public string SelectedShippingMethod { get; set; }

        public string PaymentMethodAdditionalFee { get; set; }

        public string Tax { get; set; }
        public IList<TaxRate> TaxRates { get; set; }
        public bool DisplayTax { get; set; }
        public bool DisplayTaxRates { get; set; }


        public IList<GiftVoucher> GiftVouchers { get; set; }

        public string OrderTotalDiscount { get; set; }
        public int RedeemedLoyaltyPoints { get; set; }
        public string RedeemedLoyaltyPointsAmount { get; set; }

        public int WillEarnLoyaltyPoints { get; set; }

        public string OrderTotal { get; set; }

        #region Nested classes

        public partial class TaxRate: BaseModel
        {
            public string Rate { get; set; }
            public string Value { get; set; }
        }

        public partial class GiftVoucher : BaseEntityModel
        {
            public string CouponCode { get; set; }
            public string Amount { get; set; }
            public string Remaining { get; set; }
        }
        #endregion
    }
}