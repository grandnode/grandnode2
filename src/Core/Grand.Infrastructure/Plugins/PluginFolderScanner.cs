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

public sealed record PluginLoadProblem(string Folder, string Message);

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

    private sealed record ScannedFile(string Path, string Name, Version AssemblyVersion, Version FileVersion);

    public static PluginScanResult Scan(DirectoryInfo pluginsFolder, string skipPattern, ISet<string> hostAssemblyNames)
    {
        var problems = new List<PluginLoadProblem>();
        var warnings = new List<string>();
        var plugins = new List<(PluginCandidate Candidate, List<ScannedFile> Files)>();
        var systemNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!pluginsFolder.Exists)
            return new PluginScanResult([], problems, warnings);

        var folders = pluginsFolder.GetDirectories()
            .Where(x => !IgnoredFolders.Contains(x.Name, StringComparer.OrdinalIgnoreCase))
            .OrderBy(x => x.Name, StringComparer.Ordinal);

        foreach (var folder in folders)
        {
            var files = ScanFolder(folder, skipPattern, hostAssemblyNames, warnings);

            var manifests = files
                .Select(x => (File: x, Manifest: PluginManifestReader.Read(x.Path)))
                .Where(x => x.Manifest != null)
                .ToList();

            if (manifests.Count == 0)
                continue;

            if (manifests.Count > 1)
            {
                problems.Add(new PluginLoadProblem(folder.Name,
                    $"The folder contains {manifests.Count} plugin assemblies ({string.Join(", ", manifests.Select(x => x.File.Name))}); only one is allowed."));
                continue;
            }

            var (main, manifest) = manifests[0];
            if (!systemNames.Add(manifest.SystemName ?? string.Empty))
            {
                problems.Add(new PluginLoadProblem(folder.Name,
                    $"The plugin system name '{manifest.SystemName}' is already used by another plugin folder."));
                continue;
            }

            var candidate = new PluginCandidate(folder, main.Path, manifest,
                files.Select(x => x.Path).ToList(),
                new HashSet<string>(files.Select(x => x.Name), StringComparer.OrdinalIgnoreCase));
            plugins.Add((candidate, files));
        }

        return new PluginScanResult(DropLowerVersions(plugins, warnings), problems, warnings);
    }

    private static List<ScannedFile> ScanFolder(DirectoryInfo folder, string skipPattern,
        ISet<string> hostAssemblyNames, List<string> warnings)
    {
        var result = new List<ScannedFile>();
        foreach (var file in folder.GetFiles("*.dll", SearchOption.TopDirectoryOnly)
                     .OrderBy(x => x.Name, StringComparer.Ordinal))
        {
            if (!string.IsNullOrEmpty(skipPattern) && Matches(file.Name, skipPattern))
                continue;

            if (!PluginManifestReader.IsManagedAssembly(file.FullName))
                continue;

            AssemblyName assemblyName;
            try
            {
                assemblyName = AssemblyName.GetAssemblyName(file.FullName);
            }
            catch (Exception ex) when (ex is BadImageFormatException or FileLoadException)
            {
                continue;
            }

            if (hostAssemblyNames.Contains(assemblyName.Name!))
            {
                warnings.Add(
                    $"Plugin folder '{folder.Name}' ships '{file.Name}', which the application already provides; the copy is ignored.");
                continue;
            }

            result.Add(new ScannedFile(file.FullName, assemblyName.Name!, assemblyName.Version ?? new Version(),
                ParseFileVersion(file.FullName)));
        }

        return result;
    }

    /// <summary>
    ///     When two plugins ship the same dependency only the higher version is loaded, so one load
    ///     context never sees two copies of it. A plugin's own main assembly is never dropped.
    /// </summary>
    private static List<PluginCandidate> DropLowerVersions(
        List<(PluginCandidate Candidate, List<ScannedFile> Files)> plugins, List<string> warnings)
    {
        var dropped = new Dictionary<PluginCandidate, HashSet<string>>();

        var groups = plugins
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
