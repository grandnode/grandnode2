using DotLiquid;
using Grand.Domain.Messages;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops
{
    public partial class LiquidNewsLetterSubscription : Drop
    {
        private readonly NewsLetterSubscription _subscription;
        private readonly Store _store;
        private readonly DomainHost _host;

        private string url;

        public LiquidNewsLetterSubscription(NewsLetterSubscription subscription, Store store, DomainHost host)
        {
            _subscription = subscription;
            _store = store;
            _host = host;

            url = _host?.Url.Trim('/') ?? (_store.SslEnabled ? _store.SecureUrl.Trim('/') : _store.Url.Trim('/'));

            AdditionalTokens = new Dictionary<string, string>();
        }

        public string Email
        {
            get { return _subscription.Email; }
        }

        public string ActivationUrl
        {
            get
            {
                string urlFormat = "{0}/newsletter/subscriptionactivation/{1}/{2}";
                var activationUrl = String.Format(urlFormat, url, _subscription.NewsLetterSubscriptionGuid, "true");
                return activationUrl;
            }
        }

        public string DeactivationUrl
        {
            get
            {
                string urlFormat = "{0}/newsletter/subscriptionactivation/{1}/{2}";
                var deActivationUrl = String.Format(urlFormat, url, _subscription.NewsLetterSubscriptionGuid, "false");
                return deActivationUrl;
            }
        }

        public IDictionary<string, string> AdditionalTokens { get; set; }
    }
}