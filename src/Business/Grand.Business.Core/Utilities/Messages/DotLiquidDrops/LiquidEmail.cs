using DotLiquid;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops;

public class LiquidEmail : Drop
{
    public LiquidEmail(string emailId)
    {
        Id = emailId;
    }

    public string Id { get; }
}