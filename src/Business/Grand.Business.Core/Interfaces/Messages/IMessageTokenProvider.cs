using Grand.Domain.Customers;

namespace Grand.Business.Core.Interfaces.Messages
{
    public partial interface IMessageTokenProvider
    {
        string[] GetListOfCampaignAllowedTokens();
        string[] GetListOfAllowedTokens();
        string[] GetListOfCustomerReminderAllowedTokens(CustomerReminderRuleEnum rule);
    }
}