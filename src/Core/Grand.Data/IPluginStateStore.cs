namespace Grand.Data;

/// <summary>
///     The plugins found in the Plugins folder: whether each is installed, and how its last load went. The installed
///     list is read while the application starts, before the service container and the database mappings exist, so
///     an implementation works on the driver and on plain documents, never on mapped entity classes.
/// </summary>
public interface IPluginStateStore
{
    /// <summary>
    ///     Whether there is a database to read and write; false until the application is installed
    /// </summary>
    bool HasDatabase { get; }

    /// <summary>
    ///     The system names of the installed plugins. Startup only: it runs before anything can be awaited.
    /// </summary>
    IList<string> InstalledSystemNames();

    /// <summary>
    ///     The system names of the installed plugins
    /// </summary>
    Task<IList<string>> InstalledSystemNamesAsync();

    /// <summary>
    ///     Marks the plugins installed when the store lists no plugin yet; the list of an older installation is
    ///     taken over once. Startup only.
    /// </summary>
    /// <param name="systemNames">Read only when the store lists no plugin, so a list that is ignored is never read</param>
    /// <returns>False when the store already lists plugins, and nothing was imported</returns>
    bool ImportInstalled(Func<IEnumerable<string>> systemNames);

    /// <summary>
    ///     Marks the plugin installed or uninstalled; the system name is matched ignoring case
    /// </summary>
    Task SetInstalled(string systemName, bool installed);

    /// <summary>
    ///     Removes every plugin from the store
    /// </summary>
    Task Clear();

    /// <summary>
    ///     Records what loading found for each plugin, keeping whether it is installed: a plugin the store does not
    ///     list yet is added as not installed. Startup only.
    /// </summary>
    void SaveLoadResults(IEnumerable<PluginState> plugins);
}

/// <summary>
///     A plugin as the last load found it
/// </summary>
/// <param name="SystemName">The manifest's system name, or the folder name when the plugin could not be identified</param>
/// <param name="FolderName">The folder in Plugins</param>
/// <param name="LoadError">Why the plugin was rejected; null when it was not</param>
public sealed record PluginState(
    string SystemName,
    string FriendlyName,
    string Version,
    string FolderName,
    string LoadError,
    DateTime LoadedOnUtc);

/// <summary>
///     The stored plugin document: one per system name, which is its id in lower case, since system names are
///     matched ignoring case
/// </summary>
internal static class PluginStateDocument
{
    public const string Collection = "PluginState";
    public const string Id = "_id";
    public const string SystemName = nameof(PluginState.SystemName);
    public const string Installed = "Installed";
    public const string FriendlyName = nameof(PluginState.FriendlyName);
    public const string Version = nameof(PluginState.Version);
    public const string FolderName = nameof(PluginState.FolderName);
    public const string LoadError = nameof(PluginState.LoadError);
    public const string LoadedOnUtc = nameof(PluginState.LoadedOnUtc);

    public static string IdOf(string systemName)
    {
        ArgumentException.ThrowIfNullOrEmpty(systemName);
        return systemName.ToLowerInvariant();
    }
}
