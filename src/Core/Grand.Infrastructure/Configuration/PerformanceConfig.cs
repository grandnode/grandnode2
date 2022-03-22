namespace Grand.Infrastructure.Configuration
{
    /// <summary>
    /// Represents a Performance Config
    /// </summary>
    public partial class PerformanceConfig
    {        
        /// <summary>
        /// A value indicating whether to ignore ACL rules (side-wide). It can significantly improve performance when enabled.
        /// </summary>
        public bool IgnoreAcl { get; set; }

        /// <summary>
        /// A value indicating whether to ignore "limit per store" rules (side-wide). It can significantly improve performance when enabled.
        /// </summary>
        public bool IgnoreStoreLimitations { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating for default cache time in minutes
        /// </summary>
        public int DefaultCacheTimeMinutes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether mini profiler should be displayed in public store (used for debugging)
        /// </summary>
        public bool DisplayMiniProfilerInPublicStore { get; set; }

        /// <summary>
        /// A value indicating whether to load all search engine friendly names (slugs) on application startup
        /// </summary>
        public bool LoadAllUrlEntitiesOnStartup { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether we compress response
        /// </summary>
        public bool UseResponseCompression { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether to enable html minification
        /// </summary>
        public bool UseHtmlMinification { get; set; }
        public bool HtmlMinificationErrors { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether ignore InstallUrlMiddleware
        /// </summary>
        public bool IgnoreInstallUrlMiddleware { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether ignore DbVersionCheckMiddleware
        /// </summary>
        public bool IgnoreDbVersionCheckMiddleware { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether ignore IgnoreUsePoweredByMiddleware
        /// </summary>
        public bool IgnoreUsePoweredByMiddleware { get; set; }

    }
}
