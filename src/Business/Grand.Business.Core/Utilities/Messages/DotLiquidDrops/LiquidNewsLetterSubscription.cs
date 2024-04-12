using DotLiquid;
using Grand.Domain.Messages;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops;

public class LiquidNewsLetterSubscription : Drop
{
    private readonly DomainHost _host;
    private readonly Store _store;
    private readonly NewsLetterSubscription _subscription;

    private readonly string url;

    public LiquidNewsLetterSubscription(NewsLetterSubscription subscription, Store store, DomainHost host)
    {
        _subscription = subscription;
        _store = store;
        _host = host;

        url = _host?.Url.Trim('/') ?? (_store.SslEnabled ? _store.SecureUrl.Trim('/') : _store.Url.Trim('/'));

        AdditionalTokens = new Dictionary<string, string>();
    }

    public string Email => _subscription.Email;

    public string ActivationUrl {
        get {
            var urlFormat = "{0}/newsletter/subscriptionactivation/{1}/{2}";
            var activationUrl = string.Format(urlFormat, url, _subscription.NewsLetterSubscriptionGuid, "true");
            return activationUrl;
        }
    }

    public string DeactivationUrl {
        get {
            var urlFormat = "{0}/newsletter/subscriptionactivation/{1}/{2}";
            var deActivationUrl = string.Format(urlFormat, url, _subscription.NewsLetterSubscriptionGuid, "false");
            return deActivationUrl;
        }
    }

    public IDictionary<string, string> AdditionalTokens { get; set; }
}