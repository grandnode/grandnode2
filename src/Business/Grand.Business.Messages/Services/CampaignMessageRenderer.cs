using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Business.Core.Utilities.Messages.DotLiquidDrops;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Mediator;

namespace Grand.Business.Messages.Services;

public class CampaignMessageRenderer(IMediator mediator) : ICampaignMessageRenderer
{
    public async Task<CampaignMessage> Render(Campaign campaign, Store store, Language language,
        EmailAccount emailAccount, DomainHost host, Customer customer, Currency currency,
        TaxDisplayType taxDisplayType, NewsLetterSubscription subscription = null, string queuedEmailId = null)
    {
        ArgumentNullException.ThrowIfNull(campaign);

        var builder = new LiquidObjectBuilder(mediator);
        builder.AddStoreTokens(store, language, emailAccount);
        if (subscription != null)
            builder.AddNewsLetterSubscriptionTokens(subscription, store, host);
        if (customer != null)
            builder.AddCustomerTokens(customer, store, host, language)
                .AddShoppingCartTokens(new CalculationContext(customer, store, currency, taxDisplayType), host,
                    language);

        var liquidObject = await builder.BuildAsync();
        if (queuedEmailId != null)
            liquidObject.Email = new LiquidEmail(queuedEmailId);

        var body = LiquidExtensions.Render(liquidObject, campaign.Body);
        var subject = LiquidExtensions.Render(liquidObject, campaign.Subject);

        return new CampaignMessage(subject, body);
    }
}
