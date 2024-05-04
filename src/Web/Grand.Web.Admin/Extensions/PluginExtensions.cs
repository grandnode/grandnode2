using Grand.Infrastructure;
using Grand.Infrastructure.Plugins;

namespace Grand.Web.Admin.Extensions;

public static class PluginExtensions
{
    public static string GetLogoUrl(this PluginInfo pluginDescriptor, IWorkContext workContext)
    {
        ArgumentNullException.ThrowIfNull(pluginDescriptor);
        ArgumentNullException.ThrowIfNull(workContext);

        if (pluginDescriptor.OriginalAssemblyFile?.Directory == null) return null;
        var storeLocation = workContext.CurrentHost.Url.TrimEnd('/');
        var pluginDirectory = pluginDescriptor.OriginalAssemblyFile.Directory;
        var logoPluginJpg = Path.Combine(pluginDirectory.FullName, "logo.jpg");
        if (File.Exists(logoPluginJpg))
            return $"{storeLocation}/{pluginDirectory.Parent.Name}/{pluginDirectory.Name}/logo.jpg";
        var logoPluginPng = Path.Combine(pluginDirectory.FullName, "logo.png");
        if (File.Exists(logoPluginPng))
            return $"{storeLocation}/{pluginDirectory.Parent.Name}/{pluginDirectory.Name}/logo.png";
        return null;
    }
}