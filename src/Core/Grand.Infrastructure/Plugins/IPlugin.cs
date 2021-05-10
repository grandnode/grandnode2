using System.Threading.Tasks;

namespace Grand.Infrastructure.Plugins
{
    /// <summary>
    /// Interface for Plugin
    /// the editing interface.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Gets a configuration URL
        /// </summary>
        string ConfigurationUrl();

        /// <summary>
        /// Gets or sets the plugin info
        /// </summary>
        PluginInfo PluginInfo { get; set; }

        /// <summary>
        /// Install plugin
        /// </summary>
        Task Install();

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        Task Uninstall();
    }
}
