using Grand.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Reflection;

namespace Grand.Infrastructure.Plugins
{
    public class PluginInfo
    {
        public PluginInfo(
            FileInfo originalAssemblyFile,
            Assembly referencedAssembly,
            Type pluginType)
            : this()
        {
            ReferencedAssembly = referencedAssembly;
            OriginalAssemblyFile = originalAssemblyFile;
            PluginType = pluginType;
        }
        public PluginInfo()
        {
        }

        /// <summary>
        /// Plugin type
        /// </summary>
        public virtual string PluginFileName { get; set; }

        /// <summary>
        /// Plugin type
        /// </summary>
        public virtual Type PluginType { get; set; }

        /// <summary>
        /// The assembly that has been shadow copied that is active in the application
        /// </summary>
        public virtual Assembly ReferencedAssembly { get; internal set; }

        /// <summary>
        /// The original assembly file that a shadow copy was made from it
        /// </summary>
        public virtual FileInfo OriginalAssemblyFile { get; internal set; }

        /// <summary>
        /// Gets or sets the plugin group
        /// </summary>
        public virtual string Group { get; set; }

        /// <summary>
        /// Gets or sets the friendly name
        /// </summary>
        public virtual string FriendlyName { get; set; }

        /// <summary>
        /// Gets or sets the system name
        /// </summary>
        public virtual string SystemName { get; set; }

        /// <summary>
        /// Gets or sets the version
        /// </summary>
        public virtual string Version { get; set; }

        /// <summary>
        /// Gets or sets the supported version
        /// </summary>
        public virtual string SupportedVersion { get; set; }

        /// <summary>
        /// Gets or sets the author
        /// </summary>
        public virtual string Author { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public virtual int DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether plugin is installed
        /// </summary>
        public virtual bool Installed { get; set; }

        public virtual T Instance<T>(IServiceProvider serviceProvider) where T : class, IPlugin
        {
            object instance;
            try
            {
                instance = serviceProvider.GetRequiredService(PluginType);
            }
            catch
            {
                throw new GrandException($"Plugin has not been registered getRequiredService - dependency - {PluginType.FullName}");
            }
            var typedInstance = instance as T;
            if (typedInstance != null)
                typedInstance.PluginInfo = this;
            return typedInstance;
        }

        public IPlugin Instance(IServiceProvider serviceProvider)
        {
            return Instance<IPlugin>(serviceProvider);
        }
    }
}
