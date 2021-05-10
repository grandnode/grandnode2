using Grand.Infrastructure;
using Grand.Infrastructure.Plugins;
using System;
using System.IO;

namespace Grand.Web.Admin.Extensions
{
    public static class PluginExtensions
    {
        public static string GetLogoUrl(this PluginInfo pluginDescriptor, IWorkContext workContext)
        {
            if (pluginDescriptor == null)
                throw new ArgumentNullException(nameof(pluginDescriptor));

            if (workContext == null)
                throw new ArgumentNullException(nameof(workContext));

            if (pluginDescriptor.OriginalAssemblyFile == null || pluginDescriptor.OriginalAssemblyFile.Directory == null)
            {
                return null;
            }
            var storeLocation = workContext.CurrentStore.SslEnabled ? workContext.CurrentStore.SecureUrl.TrimEnd('/') : workContext.CurrentStore.Url.TrimEnd('/');
            var pluginDirectory = pluginDescriptor.OriginalAssemblyFile.Directory;
            var logoPluginJpg = Path.Combine(pluginDirectory.FullName, "logo.jpg");
            if (File.Exists(logoPluginJpg))
            {
                return string.Format("{0}/{1}/{2}/logo.jpg", storeLocation, pluginDirectory.Parent.Name, pluginDirectory.Name);
            }
            var logoPluginPng = Path.Combine(pluginDirectory.FullName, "logo.png");
            if (File.Exists(logoPluginPng))
            {
                return string.Format("{0}/{1}/{2}/logo.png", storeLocation, pluginDirectory.Parent.Name, pluginDirectory.Name);
            }
            return null;

        }
    }
}
