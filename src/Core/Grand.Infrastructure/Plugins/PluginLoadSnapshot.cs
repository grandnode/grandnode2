using Grand.Data;
using Microsoft.Extensions.Logging;

namespace Grand.Infrastructure.Plugins;

/// <summary>
///     The plugin state this process loaded, kept to tell which changes wait for a restart. Comparing runs on
///     every admin page view, so it reads the attributes of each folder's top-level dlls, the installed plugins
///     from the database once, and the manifests of folders that had no plugin at load time and changed since -
///     never an assembly into the process.
/// </summary>
internal sealed class PluginLoadSnapshot
{
    /// <summary>
    ///     A plugin folder as it was loaded; its assemblies as a whole are compared, so a replaced dependency
    ///     counts as an update as much as a replaced main assembly
    /// </summary>
    private sealed record LoadedFolder(string SystemName, FileStamp Stamp);

    private sealed record FileStamp(long Length, DateTime LastWriteTimeUtc, int Count);

    private readonly string _pluginsFolder;
    private readonly IPluginStateStore _stateStore;
    private readonly string _skipPattern;
    private readonly HashSet<string> _installed;
    private readonly Dictionary<string, FileStamp> _otherFolders;
    private readonly Dictionary<string, LoadedFolder> _plugins;
    private readonly ILogger _logger;

    private PluginLoadSnapshot(string pluginsFolder, IPluginStateStore stateStore, string skipPattern,
        HashSet<string> installed, Dictionary<string, FileStamp> otherFolders, Dictionary<string, LoadedFolder> plugins,
        ILogger logger)
    {
        _pluginsFolder = pluginsFolder;
        _stateStore = stateStore;
        _skipPattern = skipPattern;
        _installed = installed;
        _otherFolders = otherFolders;
        _plugins = plugins;
        _logger = logger;
    }

    /// <summary>
    ///     Records what a load saw
    /// </summary>
    /// <param name="stateStore">
    ///     The store the installed list was read from; null when it came from configuration, which a running
    ///     process cannot change
    /// </param>
    /// <param name="logger">Told when the database cannot be read while comparing</param>
    internal static PluginLoadSnapshot Capture(DirectoryInfo pluginsFolder, PluginScanResult scan,
        IEnumerable<string> installedSystemNames, IPluginStateStore stateStore, string skipPattern,
        ILogger logger = null)
    {
        var plugins = new Dictionary<string, LoadedFolder>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in scan.Plugins)
            plugins[candidate.Folder.Name] = new LoadedFolder(candidate.Manifest.SystemName,
                StampOfAssemblies(candidate.Folder.FullName));

        var knownFolders = (Folders(pluginsFolder.FullName) ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);
        //a problem names its folder, or the Plugins folder itself when that could not be listed
        foreach (var problem in scan.Problems.Where(x =>
                     knownFolders.Contains(x.Folder) && !plugins.ContainsKey(x.Folder)))
            plugins[problem.Folder] = new LoadedFolder(problem.SystemNames.FirstOrDefault() ?? problem.Folder,
                StampOfAssemblies(Path.Combine(pluginsFolder.FullName, problem.Folder)));

        //a folder without a plugin (empty, or only a plugin's leftovers) is read again once its assemblies change
        var otherFolders = knownFolders.Where(x => !plugins.ContainsKey(x)).ToDictionary(x => x,
            x => StampOfAssemblies(Path.Combine(pluginsFolder.FullName, x)), StringComparer.OrdinalIgnoreCase);

        return new PluginLoadSnapshot(pluginsFolder.FullName, stateStore, skipPattern,
            installedSystemNames.ToHashSet(StringComparer.OrdinalIgnoreCase), otherFolders, plugins, logger);
    }

    /// <summary>
    ///     What differs now from the load. Never throws because of one unreadable file or folder, or a database
    ///     that cannot be read: what cannot be read is not reported.
    /// </summary>
    internal async Task<IReadOnlyList<PluginPendingChange>> Compare()
    {
        var changes = new List<PluginPendingChange>();
        await CompareInstalled(changes);
        CompareFolders(changes);
        return changes;
    }

    private async Task CompareInstalled(List<PluginPendingChange> changes)
    {
        if (_stateStore is not { HasDatabase: true })
            return;

        HashSet<string> installed;
        try
        {
            installed = (await _stateStore.InstalledSystemNamesAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            //an admin page must still render; the notice comes back with the database
            _logger?.LogWarning(ex, "The installed plugins could not be read to compare them with the loaded ones");
            return;
        }

        changes.AddRange(installed.Where(x => !_installed.Contains(x)).Order(StringComparer.OrdinalIgnoreCase)
            .Select(x => new PluginPendingChange(x, FolderOf(x), PluginPendingChangeKind.Installed)));
        changes.AddRange(_installed.Where(x => !installed.Contains(x)).Order(StringComparer.OrdinalIgnoreCase)
            .Select(x => new PluginPendingChange(x, FolderOf(x), PluginPendingChangeKind.Uninstalled)));
    }

    private void CompareFolders(List<PluginPendingChange> changes)
    {
        //a Plugins folder that cannot be listed now is not reported as every plugin removed
        var folders = Folders(_pluginsFolder)?.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (folders == null)
            return;

        foreach (var (folder, loaded) in _plugins.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            if (!folders.Contains(folder))
                changes.Add(new PluginPendingChange(loaded.SystemName, folder, PluginPendingChangeKind.Removed));
            else if (loaded.Stamp != StampOfAssemblies(Path.Combine(_pluginsFolder, folder)))
                changes.Add(new PluginPendingChange(loaded.SystemName, folder, PluginPendingChangeKind.Updated));
        }

        foreach (var folder in folders.Where(x => !_plugins.ContainsKey(x)).Order(StringComparer.Ordinal))
        {
            if (_otherFolders.TryGetValue(folder, out var stamp) &&
                stamp == StampOfAssemblies(Path.Combine(_pluginsFolder, folder)))
                continue;

            var manifest = ReadManifest(Path.Combine(_pluginsFolder, folder));
            if (manifest != null)
                changes.Add(new PluginPendingChange(
                    string.IsNullOrWhiteSpace(manifest.SystemName) ? folder : manifest.SystemName, folder,
                    PluginPendingChangeKind.Added));
        }
    }

    private string FolderOf(string systemName)
    {
        return _plugins.FirstOrDefault(x => string.Equals(x.Value.SystemName, systemName,
            StringComparison.OrdinalIgnoreCase)).Key;
    }

    /// <summary>
    ///     The first plugin manifest among the folder's top-level assemblies, read from metadata
    /// </summary>
    private PluginManifest ReadManifest(string folder)
    {
        foreach (var file in Files(folder))
        {
            try
            {
                if (PluginFolderScanner.IsSkipped(file.Name, _skipPattern))
                    continue;

                var manifest = PluginManifestReader.Read(file.FullName);
                if (manifest != null)
                    return manifest;
            }
            catch (Exception)
            {
                //any file dropped into Plugins can land here, and an admin page must still render; a file
                //still being written is read again on the next page view, a broken one fails at the restart
            }
        }

        return null;
    }

    private static FileStamp StampOfAssemblies(string folder)
    {
        var files = Files(folder);
        return new FileStamp(files.Sum(x => x.Length),
            files.Count == 0 ? DateTime.MinValue : files.Max(x => x.LastWriteTimeUtc), files.Count);
    }

    private static List<FileInfo> Files(string folder)
    {
        try
        {
            return new DirectoryInfo(folder).GetFiles("*.dll", SearchOption.TopDirectoryOnly)
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .ToList();
        }
        catch (Exception ex) when (PluginFolderScanner.IsReadFailure(ex))
        {
            return [];
        }
    }

    /// <summary>
    ///     The plugin folder names; null when the Plugins folder cannot be listed
    /// </summary>
    private static List<string> Folders(string pluginsFolder)
    {
        try
        {
            return new DirectoryInfo(pluginsFolder).GetDirectories()
                .Select(x => x.Name)
                .Where(x => !PluginFolderScanner.IsIgnoredFolder(x))
                .ToList();
        }
        catch (Exception ex) when (PluginFolderScanner.IsReadFailure(ex))
        {
            return null;
        }
    }
}
