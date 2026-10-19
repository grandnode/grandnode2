using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Business.Core.Utilities.Messages;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Messages;
using Grand.Mediator;

namespace Grand.Business.Messages.Services;

public class CampaignMessageRenderer(IMediator mediator) : ICampaignMessageRenderer
{
    public async Task<CampaignMessage> Render(Campaign campaign, CampaignRecipient recipient)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(recipient);

        var store = recipient.Store;
        var host = recipient.Host;
        var language = recipient.Language;
        var customer = recipient.Customer;

        var builder = new LiquidObjectBuilder(mediator);
        builder.AddStoreTokens(store, language, recipient.EmailAccount);
        if (recipient.Subscription != null)
            builder.AddNewsLetterSubscriptionTokens(recipient.Subscription, store, host);
        if (customer != null)
            builder.AddCustomerTokens(customer, store, host, language)
                .AddShoppingCartTokens(
                    new CalculationContext(customer, store, recipient.Currency, recipient.TaxDisplayType), host,
                    language);

        var liquidObject = await builder.BuildAsync();
        if (recipient.QueuedEmailId != null)
            liquidObject.Email = new LiquidEmail(recipient.QueuedEmailId);

        var body = LiquidExtensions.Render(liquidObject, campaign.Body);
        var subject = LiquidExtensions.Render(liquidObject, campaign.Subject);

        return new CampaignMessage(subject, body);
    }
}
