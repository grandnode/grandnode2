using Grand.Infrastructure.Plugins;

namespace Grand.Web.AdminShared.Extensions;

public static class PluginExtensions
{
    /// <summary>
    ///     Get the URL of the plugin logo, relative to the application root
    /// </summary>
    /// <param name="pluginDescriptor">Plugin</param>
    /// <param name="pathBase">Request path base (empty when the application is hosted at the root)</param>
    /// <returns>URL such as /Plugins/Payments.CashOnDelivery/logo.jpg, or null when the plugin has no logo</returns>
    public static string GetLogoUrl(this PluginInfo pluginDescriptor, string pathBase)
    {
        if (pluginDescriptor.OriginalAssemblyFile?.Directory == null) return null;
        //no scheme or host: the store's configured url can differ from the one the panel was opened on
        var root = pathBase?.TrimEnd('/') ?? string.Empty;
        var pluginDirectory = pluginDescriptor.OriginalAssemblyFile.Directory;
        var logoPluginJpg = Path.Combine(pluginDirectory.FullName, "logo.jpg");
        if (File.Exists(logoPluginJpg))
            return $"{root}/{pluginDirectory.Parent?.Name}/{pluginDirectory.Name}/logo.jpg";
        var logoPluginPng = Path.Combine(pluginDirectory.FullName, "logo.png");
        
        return File.Exists(logoPluginPng) ? $"{root}/{pluginDirectory.Parent?.Name}/{pluginDirectory.Name}/logo.png" : null;
    }
}