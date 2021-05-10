using DotLiquid;
using System.Collections.Generic;

namespace Grand.Domain.Messages
{
    /// <summary>
    /// An object that acumulates all DotLiquid Drops
    /// </summary>
    public partial class LiquidObject
    {
        public LiquidObject()
        {
            AdditionalTokens = new Dictionary<string, string>();
        }

        public Drop AttributeCombination { get; set; }

        public Drop Auctions { get; set; }

        public Drop OutOfStockSubscription { get; set; }

        public Drop BlogComment { get; set; }

        public Drop Customer { get; set; }

        public Drop GiftVoucher { get; set; }

        public Drop Knowledgebase { get; set; }

        public Drop NewsComment { get; set; }

        public Drop NewsLetterSubscription { get; set; }

        public Drop Order { get; set; }

        public Drop Product { get; set; }

        public Drop ProductReview { get; set; }

        public Drop RecurringPayment { get; set; }

        public Drop MerchandiseReturn { get; set; }

        public Drop Shipment { get; set; }

        public Drop ShoppingCart { get; set; }

        public Drop Store { get; set; }

        public Drop Vendor { get; set; }

        public Drop VendorReview { get; set; }

        public Drop EmailAFriend { get; set; }

        public Drop AskQuestion { get; set; }

        public Drop VatValidationResult { get; set; }

        public Drop ContactUs { get; set; }

        public Drop Email { get; set; }

        public IDictionary<string, string> AdditionalTokens { get; set; }
    }
}