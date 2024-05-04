namespace Grand.Business.Core.Interfaces.Messages;

public interface IMessageTokenProvider
{
    string[] GetListOfCampaignAllowedTokens();
    string[] GetListOfAllowedTokens();
}