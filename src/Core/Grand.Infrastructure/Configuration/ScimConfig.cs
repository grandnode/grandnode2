namespace Grand.Infrastructure.Configuration;

/// <summary>
///     appsettings section Scim: the SCIM 2.0 provisioning endpoint of module Grand.Module.Scim, through which the identity
///     provider of OidcResourceServer creates, updates and locks out staff accounts
/// </summary>
public class ScimConfig
{
    private const int MinTokenLength = 32;
    private const int DefaultMaxResults = 100;

    /// <summary>Bearer token the identity provider sends; keep it out of source control (env var Scim__Token or a secret store)</summary>
    public string Token { get; set; }

    /// <summary>
    ///     True: the SCIM externalId of a user is its OpenID Connect subject, and every SCIM write links the account to it,
    ///     so the first sign-in needs no password. False: accounts are linked at sign-in by the usual rules
    /// </summary>
    public bool ExternalIdIsSubject { get; set; } = true;

    /// <summary>
    ///     E-mail domains of the company (e.g. firma.pl). An existing shopper account with an e-mail in one of them is taken over
    ///     like a staff account (an employee's shopping account); any other shopper account is refused. Exact domains only, no
    ///     subdomains
    /// </summary>
    public string[] TakeOverDomains { get; set; } = [];

    /// <summary>Largest page of a list response</summary>
    public int MaxResults { get; set; } = DefaultMaxResults;

    /// <summary>A token shorter than 32 characters is a configuration mistake: the endpoint refuses every request</summary>
    public bool TokenUsable => !string.IsNullOrEmpty(Token) && Token.Length >= MinTokenLength;

    /// <summary>The e-mail is in one of <see cref="TakeOverDomains" /> (case-insensitive, exact domain)</summary>
    public bool IsTakeOverDomain(string email)
    {
        var at = email?.Trim().LastIndexOf('@') ?? -1;
        if (at < 1 || TakeOverDomains == null)
            return false;
        var domain = email.Trim()[(at + 1)..];
        return TakeOverDomains.Any(x =>
            !string.IsNullOrWhiteSpace(x) &&
            string.Equals(x.Trim().TrimStart('@'), domain, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The page size of a list request: the requested count within 0..MaxResults, MaxResults when none was given</summary>
    public int PageSize(int? requested)
    {
        var max = MaxResults > 0 ? MaxResults : DefaultMaxResults;
        return requested is null ? max : Math.Clamp(requested.Value, 0, max);
    }
}
