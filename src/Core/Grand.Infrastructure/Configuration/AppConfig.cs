namespace Grand.Infrastructure.Configuration
{
    /// <summary>
    /// Represents a Application Config
    /// </summary>
    public partial class AppConfig 
    {
        public AppConfig()
        {
            SupportedCultures = new List<string>();
        }

        /// <summary>
        /// Indicates whether we should Disable HostedService - BackgroundServiceTask
        /// </summary>
        public bool DisableHostedService { get; set; }
      
        /// <summary>
        /// A value indicating whether to ignore the migration process 
        /// </summary>
        public bool SkipMigrationProcess { get; set; }

        /// <summary>
        /// A value indicating whether SEO friendly URLs with multiple languages are enabled
        /// </summary>
        public bool SeoFriendlyUrlsForLanguagesEnabled { get; set; }
        public string SeoFriendlyUrlsDefaultCode { get; set; } = "en";
      
        /// <summary>
        /// Gets or sets a value of "Cache-Control" header value for static content
        /// </summary>
        public string StaticFilesCacheControl { get; set; }
       
        /// <summary>
        /// Gets or sets a value indicating whether to display the full error in production environment.
        /// It's ignored (always enabled) in development environment
        /// </summary>
        public bool DisplayFullErrorStack { get; set; }
        public bool UseSessionStateTempDataProvider { get; set; }
        /// <summary>
        /// Localization middleware
        /// </summary>
        public bool UseRequestLocalization { get; set; }
        public string DefaultRequestCulture { get; set; }
        public IList<string> SupportedCultures { get; set; }

        /// <summary>
        /// Gets or sets a value custom response header encoding may be needed in some cases
        /// </summary>
        public bool AllowNonAsciiCharInHeaders { get; set; }

        /// <summary>
        /// Gets or sets the maximum allowed size of any request body in bytes
        /// </summary>
        public long? MaxRequestBodySize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating - (Serilog) use middleware for smarter HTTP request logging
        /// </summary>
        public bool UseSerilogRequestLogging { get; set; }

    }
}
