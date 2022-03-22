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
        /// Indicates whether we disabled upload plugins/themes
        /// </summary>
        public bool DisableUploadExtensions { get; set; }
       
        /// <summary>
        /// Indicates whether we should Disable HostedService - BackgroundServiceTask
        /// </summary>
        public bool DisableHostedService { get; set; }
      
        /// <summary>
        /// A value indicating whether to ignore the migration process 
        /// </summary>
        public bool SkipMigrationProcess { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to clear /Plugins/bin directory on application startup
        /// </summary>
        public bool ClearPluginShadowDirectoryOnStartup { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether copy dll plugin files to /Plugins/bin on application startup
        /// </summary>
        public bool PluginShadowCopy { get; set; }
        
        
        /// <summary>
        /// A list of plugins to be ignored during start application - pattern
        /// </summary>
        public string PluginSkipLoadingPattern { get; set; }

        /// <summary>
        /// Enable scripting C# applications to execute code.
        /// </summary>
        public bool UseRoslynScripts { get; set; }
        
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
        /// Gets or sets a value indicating - (Serilog) use middleware for smarter HTTP request logging
        /// </summary>
        public bool UseSerilogRequestLogging { get; set; }

    }
}
