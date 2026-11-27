using DotLiquid;
using System.Net;

namespace Grand.Business.Messages.DotLiquidDrops;

public class LiquidEmailAFriend : Drop
{
    public LiquidEmailAFriend(string personalMessage, string customerEmail, string friendsEmail)
    {
        PersonalMessage = personalMessage;
        Email = WebUtility.HtmlEncode(customerEmail);
        FriendsEmail = WebUtility.HtmlEncode(friendsEmail);

        AdditionalTokens = new Dictionary<string, string>();
    }

    public string PersonalMessage { get; }

    public string Email { get; }

    public string FriendsEmail { get; }

    public IDictionary<string, string> AdditionalTokens { get; set; }
}