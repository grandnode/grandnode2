namespace Grand.Business.Core.Interfaces.Messages
{
    public partial interface IMessageTokenProvider
    {
        string[] GetListOfCampaignAllowedTokens();
        string[] GetListOfAllowedTokens();
    }
}