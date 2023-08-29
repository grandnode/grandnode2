using Grand.Infrastructure.Models;

namespace Grand.Web.Models.ShoppingCart
{
    public class OrderTotalsModel : BaseModel
    {
        public OrderTotalsModel()
        {
            TaxRates = new List<TaxRate>();
            GiftVouchers = new List<GiftVoucher>();
            Discounts = new HashSet<string>();
        }
        public bool IsEditable { get; set; }

        public string SubTotal { get; set; }
        public bool SubTotalIncludingTax { get; set; }
        public double SubTotalValue { get; set; }

        public string SubTotalDiscount { get; set; }
        public double SubTotalDiscountValue { get; set; }

        public string Shipping { get; set; }
        public bool RequiresShipping { get; set; }
        public string SelectedShippingMethod { get; set; }

        public string PaymentMethodAdditionalFee { get; set; }
        public double PaymentMethodAdditionalFeeValue { get; set; }

        public string Tax { get; set; }
        public IList<TaxRate> TaxRates { get; set; }
        public bool DisplayTax { get; set; }
        public bool DisplayTaxRates { get; set; }


        public IList<GiftVoucher> GiftVouchers { get; set; }

        public string OrderTotalDiscount { get; set; }
        public double OrderTotalDiscountValue { get; set; }

        public int RedeemedLoyaltyPoints { get; set; }
        public string RedeemedLoyaltyPointsAmount { get; set; }

        public int WillEarnLoyaltyPoints { get; set; }

        public string OrderTotal { get; set; }
        public double OrderTotalValue { get; set; }

        public HashSet<string> Discounts { get; set; }

        #region Nested classes

        public class TaxRate: BaseModel
        {
            public string Rate { get; set; }
            public string Value { get; set; }
        }

        public class GiftVoucher : BaseEntityModel
        {
            public string CouponCode { get; set; }
            public string Amount { get; set; }
            public string Remaining { get; set; }
        }
        #endregion
    }
}