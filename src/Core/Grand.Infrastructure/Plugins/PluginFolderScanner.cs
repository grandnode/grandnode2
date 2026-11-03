using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Grand.Infrastructure.Plugins;

/// <summary>
///     A plugin folder that can be loaded.
/// </summary>
/// <param name="MainAssemblyPath">The assembly carrying <see cref="PluginInfoAttribute" /></param>
/// <param name="AssemblyPaths">The files to load, after the higher-version dependency rule was applied</param>
/// <param name="AssemblyNames">
///     Simple names of every managed, non-host dll in the folder, including a dependency whose copy was
///     dropped in favour of another folder's higher version
/// </param>
public sealed record PluginCandidate(
    DirectoryInfo Folder,
    string MainAssemblyPath,
    PluginManifest Manifest,
    IReadOnlyList<string> AssemblyPaths,
    IReadOnlyCollection<string> AssemblyNames);

/// <summary>
///     A plugin folder that cannot be loaded.
/// </summary>
/// <param name="SystemNames">
///     The system names the folder declares, when they could be read; empty when the folder's plugin
///     cannot be identified (an unreadable or undecodable file, a manifest without a system name)
/// </param>
public sealed record PluginLoadProblem(string Folder, string Message, IReadOnlyList<string> SystemNames)
{
    public PluginLoadProblem(string folder, string message) : this(folder, message, [])
    {
    }
}

public sealed record PluginScanResult(
    IReadOnlyList<PluginCandidate> Plugins,
    IReadOnlyList<PluginLoadProblem> Problems,
    IReadOnlyList<string> Warnings);

/// <summary>
///     Plans which plugins and which of their assemblies to load. Everything is decided from file
///     metadata; no plugin assembly is loaded.
/// </summary>
public static class PluginFolderScanner
{
    private static readonly string[] IgnoredFolders = ["bin", "TempUploads"];

    private sealed record ScannedFile(string Path, string Name, Version AssemblyVersion, Version FileVersion,
        PluginManifest Manifest);

    /// <summary>
    ///     Never throws because of one folder or file: what cannot be read becomes a problem of its folder.
    /// </summary>
    public static PluginScanResult Scan(DirectoryInfo pluginsFolder, string skipPattern, ISet<string> hostAssemblyNames)
    {
        var problems = new List<PluginLoadProblem>();
        var warnings = new List<string>();
        var plugins = new List<(PluginCandidate Candidate, List<ScannedFile> Files)>();
        var systemNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!pluginsFolder.Exists)
            return new PluginScanResult([], problems, warnings);

        DirectoryInfo[] folders;
        try
        {
            folders = pluginsFolder.GetDirectories()
                .Where(x => !IgnoredFolders.Contains(x.Name, StringComparer.OrdinalIgnoreCase))
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .ToArray();
        }
        catch (Exception ex) when (IsReadFailure(ex))
        {
            problems.Add(new PluginLoadProblem(pluginsFolder.Name, $"The folder could not be read: {ex.Message}"));
            return new PluginScanResult([], problems, warnings);
        }

        foreach (var folder in folders)
        {
            var files = ScanFolder(folder, skipPattern, hostAssemblyNames, warnings, problems);
            if (files == null)
                continue;

            var manifests = files.Where(x => x.Manifest != null).ToList();

            if (manifests.Count == 0)
                continue;

            if (manifests.Count > 1)
            {
                problems.Add(new PluginLoadProblem(folder.Name,
                    $"The folder contains {manifests.Count} plugin assemblies ({string.Join(", ", manifests.Select(x => x.Name))}); only one is allowed.",
                    manifests.Select(x => x.Manifest.SystemName).Where(x => !string.IsNullOrWhiteSpace(x)).ToList()));
                continue;
            }

            var main = manifests[0];
            var manifest = main.Manifest;
            if (string.IsNullOrWhiteSpace(manifest.SystemName))
            {
                problems.Add(new PluginLoadProblem(folder.Name, "has no SystemName"));
                continue;
            }

            if (!systemNames.Add(manifest.SystemName))
            {
                problems.Add(new PluginLoadProblem(folder.Name,
                    $"The plugin system name '{manifest.SystemName}' is already used by another plugin folder.",
                    [manifest.SystemName]));
                continue;
            }

            var candidate = new PluginCandidate(folder, main.Path, manifest,
                files.Select(x => x.Path).ToList(),
                new HashSet<string>(files.Select(x => x.Name), StringComparer.OrdinalIgnoreCase));
            plugins.Add((candidate, files));
        }

        return new PluginScanResult(DropLowerVersions(plugins, warnings), problems, warnings);
    }

    /// <summary>
    ///     Returns the folder's assemblies, or null when the folder or one of its files cannot be read; the
    ///     folder is then a problem, since the unread file may be the plugin itself or one it needs.
    /// </summary>
    private static List<ScannedFile> ScanFolder(DirectoryInfo folder, string skipPattern,
        ISet<string> hostAssemblyNames, List<string> warnings, List<PluginLoadProblem> problems)
    {
        FileInfo[] dlls;
        try
        {
            dlls = folder.GetFiles("*.dll", SearchOption.TopDirectoryOnly);
        }
        catch (Exception ex) when (IsReadFailure(ex))
        {
            problems.Add(new PluginLoadProblem(folder.Name, $"The folder could not be read: {ex.Message}"));
            return null;
        }

        var result = new List<ScannedFile>();
        foreach (var file in dlls.OrderBy(x => x.Name, StringComparer.Ordinal))
        {
            if (!string.IsNullOrEmpty(skipPattern) && Matches(file.Name, skipPattern))
                continue;

            try
            {
                var scanned = ScanFile(folder, file, hostAssemblyNames, warnings);
                if (scanned != null)
                    result.Add(scanned);
            }
            catch (Exception ex) when (IsReadFailure(ex))
            {
                problems.Add(new PluginLoadProblem(folder.Name, $"'{file.Name}' could not be read: {ex.Message}"));
                return null;
            }
        }

        return result;
    }

    private static ScannedFile ScanFile(DirectoryInfo folder, FileInfo file, ISet<string> hostAssemblyNames,
        List<string> warnings)
    {
        if (!PluginManifestReader.IsManagedAssembly(file.FullName))
            return null;

        AssemblyName assemblyName;
        try
        {
            assemblyName = AssemblyName.GetAssemblyName(file.FullName);
        }
        catch (BadImageFormatException)
        {
            //metadata without an assembly manifest, such as a module: nothing to load
            return null;
        }

        if (hostAssemblyNames.Contains(assemblyName.Name!))
        {
            warnings.Add(
                $"Plugin folder '{folder.Name}' ships '{file.Name}', which the application already provides; the copy is ignored.");
            return null;
        }

        return new ScannedFile(file.FullName, assemblyName.Name!, assemblyName.Version ?? new Version(),
            ParseFileVersion(file.FullName), PluginManifestReader.Read(file.FullName));
    }

    private static bool IsReadFailure(Exception ex) =>
        ex is IOException or UnauthorizedAccessException or BadImageFormatException;

    /// <summary>
    ///     When two plugins ship the same dependency only the higher version is loaded, so one load
    ///     context never sees two copies of it. A plugin's own main assembly is never dropped. A plugin
    ///     built for another version is never loaded, so its copies take no part.
    /// </summary>
    private static List<PluginCandidate> DropLowerVersions(
        List<(PluginCandidate Candidate, List<ScannedFile> Files)> plugins, List<string> warnings)
    {
        var dropped = new Dictionary<PluginCandidate, HashSet<string>>();

        var groups = plugins
            .Where(p => p.Candidate.Manifest.SupportedVersion == GrandVersion.SupportedPluginVersion)
            .SelectMany(p => p.Files.Select(f => (p.Candidate, File: f)))
            .Where(x => !string.Equals(x.File.Path, x.Candidate.MainAssemblyPath, StringComparison.OrdinalIgnoreCase))
            .GroupBy(x => x.File.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Select(x => x.Candidate).Distinct().Count() > 1);

        foreach (var group in groups)
        {
            var winner = group
                .OrderByDescending(x => x.File.AssemblyVersion)
                .ThenByDescending(x => x.File.FileVersion)
                .First();

            foreach (var loser in group.Where(x => x != winner))
            {
                if (!dropped.TryGetValue(loser.Candidate, out var paths))
                    dropped[loser.Candidate] = paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                paths.Add(loser.File.Path);

                warnings.Add(
                    $"'{group.Key}' is shipped by '{winner.Candidate.Folder.Name}' ({winner.File.AssemblyVersion}) and '{loser.Candidate.Folder.Name}' ({loser.File.AssemblyVersion}); only the higher version is loaded.");
            }
        }

        return plugins
            .Select(p => dropped.TryGetValue(p.Candidate, out var paths)
                ? p.Candidate with { AssemblyPaths = p.Candidate.AssemblyPaths.Where(x => !paths.Contains(x)).ToList() }
                : p.Candidate)
            .ToList();
    }

    private static Version ParseFileVersion(string path)
    {
        var info = FileVersionInfo.GetVersionInfo(path);
        return new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart);
    }

    private static bool Matches(string fileName, string pattern)
    {
        return Regex.IsMatch(fileName, pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
    }
}
