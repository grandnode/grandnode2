namespace Grand.Infrastructure.Configuration
{
    public partial class SecurityConfig
    {
        /// <summary>
        /// Gets or sets a value indicating whether to use Forwards proxied headers onto current request
        /// </summary>
        public bool UseForwardedHeaders { get; set; }

        /// <summary>
        /// Gets or sets a value for allowedHosts, is used for host filtering to bind your app to specific hostnames
        /// </summary>
        public string AllowedHosts { get; set; }

        /// <summary>
        /// Gets or sets a value for Key persistence location
        /// </summary>
        public string KeyPersistenceLocation { get; set; }
        /// <summary>
        /// Gets or sets a value indicating for cookie expires in hours - default 24 * 365 = 8760
        /// </summary>
        public int CookieAuthExpires { get; set; }

        /// <summary>
        /// Gets or sets a value for Cookie prefix
        /// </summary>
        public string CookiePrefix { get; set; }

        /// <summary>
        /// Gets or sets a value for Cookie claim issuer 
        /// </summary>
        public string CookieClaimsIssuer { get; set; }

        /// <summary>
        /// Gets or sets a value of "Cookie SecurePolicy Always"
        /// </summary>
        public bool CookieSecurePolicyAlways { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether use the default security headers for your application
        /// </summary>
        public bool UseDefaultSecurityHeaders { get; set; }

        /// <summary>
        /// HTTP Strict Transport Security Protocol
        /// isn't recommended in development because the HSTS header is highly cacheable by browsers
        /// </summary>
        public bool UseHsts { get; set; }

        /// <summary>
        /// Enforce HTTPS in ASP.NET Core
        /// </summary>
        public bool UseHttpsRedirection { get; set; }

        public int HttpsRedirectionRedirect { get; set; }
        public int? HttpsRedirectionHttpsPort { get; set; }
    }
}
