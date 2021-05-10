using System.Threading.Tasks;

namespace Grand.Infrastructure.Plugins
{
    public abstract class BasePlugin : IPlugin
    {
        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        /// <returns></returns>
        public virtual string ConfigurationUrl()
        {
            return null;
        }
        /// <summary>
        /// Gets or sets the plugin info
        /// </summary>
        public virtual PluginInfo PluginInfo { get; set; }

        /// <summary>
        /// Install plugin
        /// </summary>
        public virtual async Task Install() 
        {
            await PluginExtensions.MarkPluginAsInstalled(PluginInfo.SystemName);
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public virtual async Task Uninstall() 
        {
            await PluginExtensions.MarkPluginAsUninstalled(PluginInfo.SystemName);
        }

    }
}
