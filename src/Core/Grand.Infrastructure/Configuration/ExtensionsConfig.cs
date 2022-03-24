namespace Grand.Infrastructure.Configuration
{
    /// <summary>
    /// Represents a Extensions Config
    /// </summary>
    public partial class ExtensionsConfig
    {
        /// <summary>
        /// Indicates whether we disabled upload plugins/themes
        /// </summary>
        public bool DisableUploadExtensions { get; set; }
        
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
        
    }
}
