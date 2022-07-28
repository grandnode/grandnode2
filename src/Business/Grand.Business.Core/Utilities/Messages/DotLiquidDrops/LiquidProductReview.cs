using DotLiquid;
using Grand.Domain.Catalog;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops
{
    public partial class LiquidProductReview : Drop
    {
        private readonly ProductReview _productReview;
        private readonly Product _product;

        public LiquidProductReview(Product product, ProductReview productReview)
        {
            _productReview = productReview;
            _product = product;
            AdditionalTokens = new Dictionary<string, string>();
        }

        public string ProductName
        {
            get
            {
                return _product.Name;
            }
        }

        public string ProductReviewReplyText {
            get {
                return _productReview.ReplyText;
            }
        }

        public IDictionary<string, string> AdditionalTokens { get; set; }
    }
}