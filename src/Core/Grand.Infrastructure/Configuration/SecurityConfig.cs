using Microsoft.AspNetCore.Http;

namespace Grand.Infrastructure.Configuration;

public class SecurityConfig
{
    /// <summary>
    ///     Gets or sets a value indicating whether to use Forwards proxied headers onto current request
    /// </summary>
    public bool UseForwardedHeaders { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether to force use https
    /// </summary>
    public bool ForceUseHTTPS { get; set; }

    /// <summary>
    ///     Gets or sets a value for allowedHosts, is used for host filtering to bind your app to specific hostnames
    /// </summary>
    public string AllowedHosts { get; set; }

    /// <summary>
    ///     Gets or sets a value for Key persistence location
    /// </summary>
    public string KeyPersistenceLocation { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating for cookie expires in hours - default 24 * 365 = 8760
    /// </summary>
    public int CookieAuthExpires { get; set; }

    /// <summary>
    ///     Gets or sets a value for Cookie prefix
    /// </summary>
    public string CookiePrefix { get; set; }

    /// <summary>
    ///     Gets or sets a value for Cookie SameSite
    /// </summary>
    public SameSiteMode CookieSameSite { get; set; }

    /// <summary>
    ///     Gets or sets a value for Cookie SameSite for external authentication
    /// </summary>
    public SameSiteMode CookieSameSiteExternalAuth { get; set; }

    /// <summary>
    ///     Gets or sets a value for Cookie claim issuer
    /// </summary>
    public string CookieClaimsIssuer { get; set; }

    /// <summary>
    ///     Gets or sets a value of "Cookie SecurePolicy Always"
    /// </summary>
    public bool CookieSecurePolicyAlways { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether use the default security headers for your application
    /// </summary>
    public bool UseDefaultSecurityHeaders { get; set; }

    /// <summary>
    ///     HTTP Strict Transport Security Protocol
    ///     isn't recommended in development because the HSTS header is highly cacheable by browsers
    /// </summary>
    public bool UseHsts { get; set; }

    /// <summary>
    ///     Enforce HTTPS in ASP.NET Core
    /// </summary>
    public bool UseHttpsRedirection { get; set; }

    public int HttpsRedirectionRedirect { get; set; }
    public int? HttpsRedirectionHttpsPort { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether to verify access to a specific controller and action in the admin panel
    ///     using menu configuration.
    /// </summary>
    public bool AuthorizeAdminMenu { get; set; }

    /// <summary>
    ///     Server-side secret ("pepper") mixed into the PBKDF2 password hash. Optional but recommended: it must be stored
    ///     outside the database (appsettings/secret store), so a database-only leak is not enough to verify hashes offline.
    ///     Leave empty to hash without a pepper.
    ///     IMPORTANT: changing this value invalidates all existing PBKDF2 hashes (affected customers must reset their
    ///     password); legacy SHA hashes are unaffected. Set it once, before going live.
    /// </summary>
    public string PasswordHashKey { get; set; }

    /// <summary>
    ///     PBKDF2 (HMAC-SHA256) iteration count for newly created/upgraded password hashes. Default 210000 (OWASP 2023).
    ///     The value is embedded in each stored hash, so raising it later does not break existing hashes.
    /// </summary>
    public int PasswordHashIterations { get; set; }

    /// <summary>
    ///     Hosts whose iframes survive HTML sanitization of rich-text content (product descriptions, blog posts, pages).
    ///     An iframe pointing anywhere else is removed, because its src is otherwise attacker-controlled.
    ///     Matching is case-insensitive on the host only; a leading "*." matches any subdomain.
    ///     Leave empty to fall back to the built-in video-embed defaults; set to a single empty entry to block every iframe.
    /// </summary>
    public string[] SanitizerAllowedIframeHosts { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether [SanitizeHtml] and [NoHtml] reject markup on save. Default true.
    ///     This is an operational escape hatch, not a security setting: turn it off only temporarily, if the
    ///     allowlist is found to reject legitimate content in production, while a fix is prepared - every field
    ///     these attributes guard (Vendor/Store-manager-editable rich text rendered unencoded via Html.Raw/v-html)
    ///     goes back to accepting raw, unsanitized HTML the moment this is false. Re-enable as soon as possible.
    /// </summary>
    public bool EnableHtmlSanitization { get; set; } = true;
}