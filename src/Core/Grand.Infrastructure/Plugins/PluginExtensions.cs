using Grand.SharedKernel.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Grand.Infrastructure.Plugins
{
    public static class PluginExtensions
    {
        public static bool OnlyInstalledPlugins(Type type)
        {
            var value = true;
            var plugin = PluginManager.FindPlugin(type);
            if (plugin != null)
            {
                return plugin.Installed;
            }
            return value;
        }

        public static IList<string> ParseInstalledPluginsFile(string filePath)
        {
            if (!File.Exists(filePath))
                return new List<string>();

            var text = File.ReadAllText(filePath);
            if (String.IsNullOrEmpty(text))
                return new List<string>();

            return JsonSerializer.Deserialize<List<string>>(text);
        }

        public static async Task SaveInstalledPluginsFile(IList<string> pluginSystemNames, string filePath)
        {
            //serialize
            string result = JsonSerializer.Serialize(pluginSystemNames, new JsonSerializerOptions { WriteIndented = true });
            //save
            await File.WriteAllTextAsync(filePath, result);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Mark plugin as installed
        /// </summary>
        /// <param name="systemName">Plugin system name</param>
        public static async Task MarkPluginAsInstalled(string systemName)
        {
            if (string.IsNullOrEmpty(systemName))
                throw new ArgumentNullException("systemName");

            var filePath = CommonPath.InstalledPluginsFilePath;
            if (!File.Exists(filePath))
                using (File.Create(filePath))
                {
                    //we use 'using' to close the file after it's created
                }

            var installedPluginSystemNames = ParseInstalledPluginsFile(filePath);

            var alreadyMarkedAsInstalled = installedPluginSystemNames.FirstOrDefault(x => x.Equals(systemName, StringComparison.OrdinalIgnoreCase)) != null;
            if (!alreadyMarkedAsInstalled)
                installedPluginSystemNames.Add(systemName);

            await SaveInstalledPluginsFile(installedPluginSystemNames, filePath);
        }

        /// <summary>
        /// Mark plugin as uninstalled
        /// </summary>
        /// <param name="systemName">Plugin system name</param>
        public static async Task MarkPluginAsUninstalled(string systemName)
        {
            if (string.IsNullOrEmpty(systemName))
                throw new ArgumentNullException(nameof(systemName));

            var filePath = CommonPath.InstalledPluginsFilePath;
            if (!File.Exists(filePath))
                using (File.Create(filePath))
                {
                }

            var installedPluginSystemNames = ParseInstalledPluginsFile(filePath);
            var alreadyMarkedAsInstalled = installedPluginSystemNames.FirstOrDefault(x => x.Equals(systemName, StringComparison.OrdinalIgnoreCase)) != null;
            if (alreadyMarkedAsInstalled)
                installedPluginSystemNames.Remove(systemName);

            await SaveInstalledPluginsFile(installedPluginSystemNames, filePath);
        }


    }
}
