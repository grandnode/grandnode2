using Grand.Business.Core.Utilities.Messages;
using Grand.Domain.Messages;

namespace Grand.Business.Core.Interfaces.Messages;

/// <summary>
///     Renders the subject and body of a newsletter campaign with the message tokens
/// </summary>
public interface ICampaignMessageRenderer
{
    /// <summary>
    ///     Renders the campaign for one recipient
    /// </summary>
    /// <param name="campaign">Campaign</param>
    /// <param name="recipient">Recipient and the context the message is rendered in</param>
    Task<CampaignMessage> Render(Campaign campaign, CampaignRecipient recipient);
}
