using DotLiquid;
using Grand.Domain.Catalog;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops
{
    public partial class LiquidAttributeCombination : Drop
    {
        private readonly ProductAttributeCombination _combination;

        public LiquidAttributeCombination(ProductAttributeCombination combination)
        {
            _combination = combination;
            AdditionalTokens = new Dictionary<string, string>();
        }

        public string Formatted { get; set; }

        public string SKU { get; set; }

        public string StockQuantity
        {
            get { return _combination.StockQuantity.ToString(); }
        }

        public IDictionary<string, string> AdditionalTokens { get; set; }
    }
}
