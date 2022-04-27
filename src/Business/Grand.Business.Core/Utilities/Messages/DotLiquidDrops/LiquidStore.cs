using DotLiquid;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops
{
    public partial class LiquidStore : Drop
    {
        private readonly Store _store;
        private readonly EmailAccount _emailAccount;
        private readonly Language _language;

        public LiquidStore(Store store, Language language, EmailAccount emailAccount = null)
        {
            _store = store;
            _language = language;
            _emailAccount = emailAccount;

            AdditionalTokens = new Dictionary<string, string>();
        }

        public string Name {
            get { return _store.Name; }
        }

        public string Shortcut {
            get { return _store.Shortcut; }
        }

        public string URL {
            get { return _store.SslEnabled ? _store.SecureUrl : _store.Url; }
        }

        public string Email {
            get { return _emailAccount.Email; }
        }

        public string CompanyName {
            get { return _store.CompanyName; }
        }

        public string CompanyAddress {
            get { return _store.CompanyAddress; }
        }

        public string CompanyPhoneNumber {
            get { return _store.CompanyPhoneNumber; }
        }

        public string CompanyEmail {
            get { return _store.CompanyEmail; }
        }

        public string CompanyHours {
            get { return _store.CompanyHours; }
        }

        public string CompanyRegNo {
            get { return _store.CompanyRegNo; }
        }

        public string CompanyVat {
            get { return _store.CompanyVat; }
        }

        public string BankCode {
            get { return _store.BankAccount?.BankCode; }
        }
        public string BankName {
            get { return _store.BankAccount?.BankName; }
        }
        public string SwiftCode {
            get { return _store.BankAccount?.SwiftCode; }
        }
        public string AccountNumber {
            get { return _store.BankAccount?.AccountNumber; }
        }

        public string TwitterLink { get; set; }

        public string FacebookLink { get; set; }

        public string YoutubeLink { get; set; }

        public string InstagramLink { get; set; }

        public string LinkedInLink { get; set; }

        public string PinterestLink { get; set; }

        public IDictionary<string, string> AdditionalTokens { get; set; }
    }
}