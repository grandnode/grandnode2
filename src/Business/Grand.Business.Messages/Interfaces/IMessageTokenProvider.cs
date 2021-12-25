using Grand.Domain.Customers;

namespace Grand.Business.Messages.Interfaces
{
    public partial interface IMessageTokenProvider
    {
        string[] GetListOfCampaignAllowedTokens();
        string[] GetListOfAllowedTokens();
        string[] GetListOfCustomerReminderAllowedTokens(CustomerReminderRuleEnum rule);
    }
}