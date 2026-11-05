namespace Grand.Infrastructure.Plugins;

/// <summary>
///     What changed on disk since this process loaded its plugins
/// </summary>
public enum PluginPendingChangeKind
{
    Installed,
    Uninstalled,
    Added,
    Removed,
    Updated
}

/// <summary>
///     A plugin change that takes effect after the application restarts
/// </summary>
/// <param name="SystemName">The plugin's system name; the folder name when the system name is not known</param>
/// <param name="FolderName">The plugin folder; null when the plugin has no folder this process knows</param>
public sealed record PluginPendingChange(string SystemName, string FolderName, PluginPendingChangeKind Kind);
