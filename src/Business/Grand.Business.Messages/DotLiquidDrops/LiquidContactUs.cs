using DotLiquid;
using System.Net;

namespace Grand.Business.Messages.DotLiquidDrops;

public class LiquidContactUs : Drop
{
    public LiquidContactUs(string senderEmail, string senderName, string body, string attributeDescription)
    {
        SenderEmail = WebUtility.HtmlEncode(senderEmail);
        SenderName = WebUtility.HtmlEncode(senderName);
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