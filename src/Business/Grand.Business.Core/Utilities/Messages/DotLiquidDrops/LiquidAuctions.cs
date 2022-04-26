using DotLiquid;
using Grand.Domain.Catalog;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops
{
    public partial class LiquidAuctions : Drop
    {
        private readonly Product _product;
        private readonly Bid _bid;

        public LiquidAuctions(Product product, Bid bid = null)
        {
            _product = product;
            _bid = bid;

            AdditionalTokens = new Dictionary<string, string>();
        }

        public string ProductName
        {
            get { return _product.Name; }
        }

        public string Price { get; set; }

        public string EndTime { get; set; }

        public string ProductSeName
        {
            get { return _product.SeName; }
        }

        public IDictionary<string, string> AdditionalTokens { get; set; }
    }
}