using DotLiquid;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops;

public class LiquidVatValidationResult : Drop
{
    public LiquidVatValidationResult(string name, string address)
    {
        Name = name;
        Address = address;

        AdditionalTokens = new Dictionary<string, string>();
    }

    public string Name { get; }

    public string Address { get; }

    public IDictionary<string, string> AdditionalTokens { get; set; }
}