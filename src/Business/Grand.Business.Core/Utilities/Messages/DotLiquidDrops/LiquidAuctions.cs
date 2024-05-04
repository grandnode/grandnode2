using DotLiquid;
using Grand.Domain.Catalog;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops;

public class LiquidAuctions : Drop
{
    private readonly Bid _bid;
    private readonly Product _product;

    public LiquidAuctions(Product product, Bid bid = null)
    {
        _product = product;
        _bid = bid;

        AdditionalTokens = new Dictionary<string, string>();
    }

    public string ProductName => _product.Name;

    public string Price { get; set; }

    public string EndTime { get; set; }

    public string ProductSeName => _product.SeName;

    public IDictionary<string, string> AdditionalTokens { get; set; }
}