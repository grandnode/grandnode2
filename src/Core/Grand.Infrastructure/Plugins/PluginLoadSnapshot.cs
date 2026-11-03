using Grand.SharedKernel;

namespace Grand.Infrastructure.Plugins;

/// <summary>
///     The plugin state this process loaded, kept to tell which changes on disk wait for a restart. Comparing
///     runs on every admin page view, so it reads file attributes, the installed-plugins file once, and the
///     manifests of folders that did not exist at load time - never an assembly into the process.
/// </summary>
internal sealed class PluginLoadSnapshot
{
    /// <summary>
    ///     A plugin folder as it was loaded. A folder that could not be loaded has no main assembly; its
    ///     plugin assemblies as a whole are compared instead.
    /// </summary>
    private sealed record LoadedFolder(string SystemName, string MainAssemblyPath, FileStamp Stamp);

    private sealed record FileStamp(long Length, DateTime LastWriteTimeUtc, int Count);

    private readonly string _pluginsFolder;
    private readonly string _installedPluginsFile;
    private readonly string _skipPattern;
    private readonly HashSet<string> _installed;
    private readonly HashSet<string> _knownFolders;
    private readonly Dictionary<string, LoadedFolder> _plugins;

    private PluginLoadSnapshot(string pluginsFolder, string installedPluginsFile, string skipPattern,
        HashSet<string> installed, HashSet<string> knownFolders, Dictionary<string, LoadedFolder> plugins)
    {
        _pluginsFolder = pluginsFolder;
        _installedPluginsFile = installedPluginsFile;
        _skipPattern = skipPattern;
        _installed = installed;
        _knownFolders = knownFolders;
        _plugins = plugins;
    }

    /// <summary>
    ///     Records what a load saw
    /// </summary>
    /// <param name="installedPluginsFile">
    ///     The file the installed list was read from; null when it came from configuration, which a running
    ///     process cannot change
    /// </param>
    internal static PluginLoadSnapshot Capture(DirectoryInfo pluginsFolder, PluginScanResult scan,
        IEnumerable<string> installedSystemNames, string installedPluginsFile, string skipPattern)
    {
        var plugins = new Dictionary<string, LoadedFolder>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in scan.Plugins)
            plugins[candidate.Folder.Name] = new LoadedFolder(candidate.Manifest.SystemName,
                candidate.MainAssemblyPath, StampOf(candidate.MainAssemblyPath));

        var knownFolders = (Folders(pluginsFolder.FullName) ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);
        //a problem names its folder, or the Plugins folder itself when that could not be listed
        foreach (var problem in scan.Problems.Where(x =>
                     knownFolders.Contains(x.Folder) && !plugins.ContainsKey(x.Folder)))
            plugins[problem.Folder] = new LoadedFolder(problem.SystemNames.FirstOrDefault() ?? problem.Folder, null,
                StampOfAssemblies(Path.Combine(pluginsFolder.FullName, problem.Folder)));

        return new PluginLoadSnapshot(pluginsFolder.FullName, installedPluginsFile, skipPattern,
            installedSystemNames.ToHashSet(StringComparer.OrdinalIgnoreCase), knownFolders, plugins);
    }

    /// <summary>
    ///     What differs now from the load. Never throws because of one unreadable file or folder: what
    ///     cannot be read is not reported.
    /// </summary>
    internal IReadOnlyList<PluginPendingChange> Compare()
    {
        var changes = new List<PluginPendingChange>();
        CompareInstalled(changes);
        CompareFolders(changes);
        return changes;
    }

    private void CompareInstalled(List<PluginPendingChange> changes)
    {
        if (_installedPluginsFile == null)
            return;

        HashSet<string> installed;
        try
        {
            installed = PluginExtensions.ParseInstalledPluginsFile(_installedPluginsFile)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is GrandException || PluginFolderScanner.IsReadFailure(ex))
        {
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
            else if (loaded.Stamp != CurrentStamp(folder, loaded))
                changes.Add(new PluginPendingChange(loaded.SystemName, folder, PluginPendingChangeKind.Updated));
        }

        foreach (var folder in folders.Where(x => !_knownFolders.Contains(x)).Order(StringComparer.Ordinal))
        {
            var manifest = ReadManifest(Path.Combine(_pluginsFolder, folder));
            if (manifest != null)
                changes.Add(new PluginPendingChange(
                    string.IsNullOrWhiteSpace(manifest.SystemName) ? folder : manifest.SystemName, folder,
                    PluginPendingChangeKind.Added));
        }
    }

    private FileStamp CurrentStamp(string folder, LoadedFolder loaded)
    {
        return loaded.MainAssemblyPath != null
            ? StampOf(loaded.MainAssemblyPath)
            : StampOfAssemblies(Path.Combine(_pluginsFolder, folder));
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

    private static FileStamp StampOf(string path)
    {
        try
        {
            var file = new FileInfo(path);
            return file.Exists ? new FileStamp(file.Length, file.LastWriteTimeUtc, 1) : null;
        }
        catch (Exception ex) when (PluginFolderScanner.IsReadFailure(ex))
        {
            return null;
        }
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
