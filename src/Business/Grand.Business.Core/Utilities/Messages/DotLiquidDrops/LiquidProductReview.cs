using DotLiquid;
using Grand.Domain.Catalog;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops;

public class LiquidProductReview : Drop
{
    private readonly Product _product;
    private readonly ProductReview _productReview;

    public LiquidProductReview(Product product, ProductReview productReview)
    {
        _productReview = productReview;
        _product = product;
        AdditionalTokens = new Dictionary<string, string>();
    }

    public string ProductName => _product.Name;

    public string ProductReviewReplyText => _productReview.ReplyText;

    public IDictionary<string, string> AdditionalTokens { get; set; }
}