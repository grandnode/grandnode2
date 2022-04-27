using DotLiquid;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops
{
    public partial class LiquidVatValidationResult : Drop
    {
        private readonly string name;
        private readonly string address;

        public LiquidVatValidationResult(string name, string address)
        {
            this.name = name;
            this.address = address;

            AdditionalTokens = new Dictionary<string, string>();
        }

        public string Name
        {
            get { return name; }
        }

        public string Address
        {
            get { return address; }
        }

        public IDictionary<string, string> AdditionalTokens { get; set; }
    }
}