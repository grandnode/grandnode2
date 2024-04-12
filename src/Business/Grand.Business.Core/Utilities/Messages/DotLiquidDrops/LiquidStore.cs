using DotLiquid;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops;

public class LiquidStore : Drop
{
    private readonly EmailAccount _emailAccount;
    private readonly Language _language;
    private readonly Store _store;

    public LiquidStore(Store store, Language language, EmailAccount emailAccount = null)
    {
        _store = store;
        _language = language;
        _emailAccount = emailAccount;

        AdditionalTokens = new Dictionary<string, string>();
    }

    public string Name => _store.Name;

    public string Shortcut => _store.Shortcut;

    public string URL => _store.SslEnabled ? _store.SecureUrl : _store.Url;

    public string Email => _emailAccount.Email;

    public string CompanyName => _store.CompanyName;

    public string CompanyAddress => _store.CompanyAddress;

    public string CompanyPhoneNumber => _store.CompanyPhoneNumber;

    public string CompanyEmail => _store.CompanyEmail;

    public string CompanyHours => _store.CompanyHours;

    public string CompanyRegNo => _store.CompanyRegNo;

    public string CompanyVat => _store.CompanyVat;

    public string BankCode => _store.BankAccount?.BankCode;

    public string BankName => _store.BankAccount?.BankName;

    public string SwiftCode => _store.BankAccount?.SwiftCode;

    public string AccountNumber => _store.BankAccount?.AccountNumber;

    public string TwitterLink { get; set; }

    public string FacebookLink { get; set; }

    public string YoutubeLink { get; set; }

    public string InstagramLink { get; set; }

    public string LinkedInLink { get; set; }

    public string PinterestLink { get; set; }

    public IDictionary<string, string> AdditionalTokens { get; set; }
}