using Grand.Infrastructure.Plugins;

namespace Grand.Web.Admin.Extensions;

public static class PluginExtensions
{
    public static string GetLogoUrl(this PluginInfo pluginDescriptor, string url)
    {
        if (pluginDescriptor.OriginalAssemblyFile?.Directory == null) return null;
        var storeLocation = url.TrimEnd('/');
        var pluginDirectory = pluginDescriptor.OriginalAssemblyFile.Directory;
        var logoPluginJpg = Path.Combine(pluginDirectory.FullName, "logo.jpg");
        if (File.Exists(logoPluginJpg))
            return $"{storeLocation}/{pluginDirectory.Parent?.Name}/{pluginDirectory.Name}/logo.jpg";
        var logoPluginPng = Path.Combine(pluginDirectory.FullName, "logo.png");
        
        return File.Exists(logoPluginPng) ? $"{storeLocation}/{pluginDirectory.Parent?.Name}/{pluginDirectory.Name}/logo.png" : null;
    }
}