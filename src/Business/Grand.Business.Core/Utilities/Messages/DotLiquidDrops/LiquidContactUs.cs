using DotLiquid;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops;

public class LiquidContactUs : Drop
{
    public LiquidContactUs(string senderEmail, string senderName, string body, string attributeDescription)
    {
        SenderEmail = senderEmail;
        SenderName = senderName;
        Body = body;
        AttributeDescription = attributeDescription;

        AdditionalTokens = new Dictionary<string, string>();
    }

    public string SenderEmail { get; }

    public string SenderName { get; }

    public string Body { get; }

    public string AttributeDescription { get; }

    public IDictionary<string, string> AdditionalTokens { get; set; }
}