using DotLiquid;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops;

public class LiquidAskQuestion : Drop
{
    public LiquidAskQuestion(string message, string email, string fullName, string phone)
    {
        Message = message;
        Email = email;
        FullName = fullName;
        Phone = phone;

        AdditionalTokens = new Dictionary<string, string>();
    }

    public string Email { get; }

    public string Message { get; }

    public string FullName { get; }

    public string Phone { get; }

    public IDictionary<string, string> AdditionalTokens { get; set; }
}