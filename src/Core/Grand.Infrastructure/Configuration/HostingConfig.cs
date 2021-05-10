namespace Grand.Infrastructure.Configuration
{
    public partial class HostingConfig
    {
        /// <summary>
        /// Gets or sets a value indicating whether to use Forwards proxied headers onto current request
        /// </summary>
        public bool UseForwardedHeaders { get; set; }

        /// <summary>
        /// Gets or sets a value for allowedHosts, is used for host filtering to bind your app to specific hostnames
        /// </summary>
        public string AllowedHosts { get; set; }
    }
}
