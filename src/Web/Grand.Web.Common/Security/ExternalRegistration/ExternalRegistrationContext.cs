using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Customers;
using Grand.Web.Common.Security.Oidc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Grand.Web.Common.Security.ExternalRegistration;

public class ExternalRegistrationContext : IExternalRegistrationContext
{
    /// <summary>Authentication type of the pending identity; other external logins in the same cookie are ignored</summary>
    public const string AuthenticationType = "GrandExternalRegistration";

    private const string ProviderClaim = "grand_ext_provider";
    private const string SubjectClaim = "grand_ext_sub";
    private const string EmailClaim = "grand_ext_email";
    private const string EmailVerifiedClaim = "grand_ext_email_verified";
    private const string FirstNameClaim = "grand_ext_given_name";
    private const string LastNameClaim = "grand_ext_family_name";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOidcCustomerResolver _oidcCustomerResolver;
    private readonly ILogger<ExternalRegistrationContext> _logger;

    public ExternalRegistrationContext(IHttpContextAccessor httpContextAccessor, IOidcCustomerResolver oidcCustomerResolver,
        ILogger<ExternalRegistrationContext> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _oidcCustomerResolver = oidcCustomerResolver;
        _logger = logger;
    }

    public static ClaimsPrincipal CreatePrincipal(ExternalRegistrationIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        var claims = new List<Claim> {
            new(ProviderClaim, identity.ProviderSystemName),
            new(SubjectClaim, identity.Subject),
            new(EmailClaim, identity.Email ?? ""),
            new(EmailVerifiedClaim, identity.EmailVerified ? "true" : "false")
        };
        if (!string.IsNullOrEmpty(identity.FirstName)) claims.Add(new Claim(FirstNameClaim, identity.FirstName));
        if (!string.IsNullOrEmpty(identity.LastName)) claims.Add(new Claim(LastNameClaim, identity.LastName));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationType));
    }

    public async Task<ExternalRegistrationIdentity> GetPending()
    {
        var result = await HttpContext.AuthenticateAsync(GrandCookieAuthenticationDefaults.ExternalAuthenticationScheme);
        var principal = result.Succeeded ? result.Principal : null;
        if (principal?.Identity?.AuthenticationType != AuthenticationType)
            return null;

        var provider = principal.FindFirst(ProviderClaim)?.Value;
        var subject = principal.FindFirst(SubjectClaim)?.Value;
        var email = principal.FindFirst(EmailClaim)?.Value;
        if (string.IsNullOrEmpty(provider) || string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(email))
            return null;

        return new ExternalRegistrationIdentity(provider, subject, email,
            principal.FindFirst(EmailVerifiedClaim)?.Value == "true",
            principal.FindFirst(FirstNameClaim)?.Value, principal.FindFirst(LastNameClaim)?.Value);
    }

    public Task Clear() => HttpContext.SignOutAsync(GrandCookieAuthenticationDefaults.ExternalAuthenticationScheme);

    public async Task<OidcLinkResult> Complete(Customer customer, string storeId)
    {
        ArgumentNullException.ThrowIfNull(customer);
        var pending = await GetPending() ?? throw new InvalidOperationException("No pending external identity");
        try
        {
            if (pending.ProviderSystemName != OidcResourceServerDefaults.ProviderSystemName)
                throw new InvalidOperationException($"Unsupported external provider {pending.ProviderSystemName}");

            var result = await _oidcCustomerResolver.LinkIdentity(customer, pending.Subject, pending.Email, storeId);
            if (result != OidcLinkResult.Linked)
                _logger.LogWarning("Pending identity {Subject} not connected to customer {CustomerId}: {Result}",
                    pending.Subject, customer.Id, result);
            return result;
        }
        finally
        {
            await Clear();
        }
    }

    private HttpContext HttpContext =>
        _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HTTP request");
}
