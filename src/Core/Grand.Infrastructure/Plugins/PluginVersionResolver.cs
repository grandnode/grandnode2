using System.Reflection;

namespace Grand.Infrastructure.Plugins;

/// <summary>
///     Resolves which GrandNode version a plugin assembly was built against.
/// </summary>
public static class PluginVersionResolver
{
    private const string CoreAssemblyName = "Grand.Infrastructure";

    /// <summary>
    ///     Returns the GrandNode version the plugin supports, as "Major.Minor".
    /// </summary>
    /// <param name="pluginAssembly">The plugin assembly the info attribute was read from</param>
    /// <param name="declaredVersion">The version declared on <see cref="PluginInfoAttribute" />, if any</param>
    /// <returns>
    ///     The declared version when the plugin states one, otherwise the version of the
    ///     <see cref="CoreAssemblyName" /> reference it was compiled against. Null when neither is available -
    ///     such a plugin cannot be matched against <see cref="GrandVersion.SupportedPluginVersion" /> and is
    ///     therefore treated as incompatible.
    /// </returns>
    public static string ResolveSupportedVersion(Assembly pluginAssembly, string declaredVersion)
    {
        var coreReference = pluginAssembly?.GetReferencedAssemblies()
            .FirstOrDefault(x => string.Equals(x.Name, CoreAssemblyName, StringComparison.OrdinalIgnoreCase));

        return ResolveSupportedVersion(coreReference?.Version, declaredVersion);
    }

    /// <summary>
    ///     Same rule as the <see cref="Assembly" /> overload, for callers that read the reference from metadata.
    /// </summary>
    /// <param name="coreReference">The version of the plugin's Grand.Infrastructure reference, if it has one</param>
    /// <param name="declaredVersion">The version declared on <see cref="PluginInfoAttribute" />, if any</param>
    public static string ResolveSupportedVersion(Version coreReference, string declaredVersion)
    {
        if (!string.IsNullOrWhiteSpace(declaredVersion))
            return declaredVersion.Trim();

        return coreReference == null
            ? null
            : $"{coreReference.Major}.{coreReference.Minor}";
    }
}
