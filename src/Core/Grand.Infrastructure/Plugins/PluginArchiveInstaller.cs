using System.IO.Compression;
using Grand.SharedKernel;
using Grand.SharedKernel.Extensions;
using Microsoft.Extensions.Logging;

namespace Grand.Infrastructure.Plugins;

/// <summary>
///     Installs an uploaded plugin archive. The plugin is identified by the manifest in its own folder, never by
///     loading its assemblies, and the target folder is swapped in one step so a failure leaves the previous
///     version in place.
/// </summary>
public sealed class PluginArchiveInstaller(string pluginsPath, ILogger logger)
{
    public const int MaxEntries = 5000;
    public const long MaxUncompressedBytes = 200L * 1024 * 1024;

    private const string PluginsFolderName = "Plugins";
    private const string BinFolderName = "bin";
    private const string ExtractedFolderName = "x";
    private const string PreviousFolderName = "previous";

    /// <summary>
    ///     Validates the archive and installs the plugin it contains into the plugins folder.
    /// </summary>
    /// <param name="archivePath">Path of the zip file</param>
    /// <exception cref="GrandException">The archive is not an installable plugin; the message is meant for the admin</exception>
    public PluginArchiveResult Install(string archivePath)
    {
        var tempRoot = Path.Combine(pluginsPath, CommonPath.TmpUploadPath, Guid.NewGuid().ToString());
        try
        {
            CheckArchiveSize(archivePath);

            var extracted = Path.Combine(tempRoot, ExtractedFolderName);
            Extract(archivePath, extracted);
            NormalizeBackslashes(extracted);

            var (folderName, sourceFolder) = FindPluginFolder(extracted);
            var manifest = ReadSingleManifest(sourceFolder);

            if (!string.Equals(manifest.SupportedVersion, GrandVersion.SupportedPluginVersion, StringComparison.Ordinal))
                throw new GrandException(
                    $"This plugin doesn't support the current version - {GrandVersion.SupportedPluginVersion}");

            if (IsSystemNameUsedByAnotherFolder(manifest.SystemName, folderName))
                throw new GrandException(
                    $"A plugin with the system name '{manifest.SystemName}' is already installed in another folder.");

            Replace(sourceFolder, Path.Combine(pluginsPath, folderName), Path.Combine(tempRoot, PreviousFolderName));
            return new PluginArchiveResult(folderName, manifest);
        }
        finally
        {
            DeleteTempRoot(tempRoot);
        }
    }

    private static void CheckArchiveSize(string archivePath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(archivePath);
            if (archive.Entries.Count > MaxEntries)
                throw new GrandException($"The archive contains too many files (the limit is {MaxEntries}).");

            long total = 0;
            foreach (var entry in archive.Entries)
            {
                total += entry.Length;
                if (total > MaxUncompressedBytes)
                    throw new GrandException(
                        $"The archive is too large when unpacked (the limit is {MaxUncompressedBytes / 1024 / 1024} MB).");
            }
        }
        catch (InvalidDataException)
        {
            throw new GrandException("The file is not a valid zip archive.");
        }
    }

    private static void Extract(string archivePath, string destination)
    {
        Directory.CreateDirectory(destination);
        try
        {
            //refuses entries that would land outside the destination
            ZipFile.ExtractToDirectory(archivePath, destination);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            throw new GrandException("The archive could not be unpacked. Check that it is a valid plugin archive.");
        }
    }

    /// <summary>
    ///     Where '\' is not the directory separator, an entry written with backslashes is extracted as one
    ///     file with backslashes in its name; move it to the path it stands for.
    /// </summary>
    private static void NormalizeBackslashes(string root)
    {
        if (Path.DirectorySeparatorChar == '\\')
            return;

        var rootFull = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                     .Where(x => Path.GetFileName(x).Contains('\\')).ToList())
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', Path.DirectorySeparatorChar);
            var destination = Path.GetFullPath(Path.Combine(root, relative));
            if (!destination.StartsWith(rootFull, StringComparison.Ordinal))
                throw new GrandException("The archive contains a path outside of the plugin folder.");

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Move(file, destination, true);
        }
    }

    /// <summary>
    ///     The plugin folder is the one top-level folder holding the files; an archive that wraps it in a
    ///     <c>Plugins</c> folder is unwrapped first. The folder is derived from the files, not from directory entries.
    /// </summary>
    private static (string Name, string Path) FindPluginFolder(string extracted)
    {
        var root = extracted;
        var folders = TopLevelFolders(root);

        if (folders.Count == 1 && string.Equals(folders[0], PluginsFolderName, StringComparison.OrdinalIgnoreCase))
        {
            root = Path.Combine(root, folders[0]);
            folders = TopLevelFolders(root);
        }

        if (folders.Count != 1)
            throw new GrandException(
                "The archive should contain only one root plugin folder. For example, Payments.StripeCheckout.");

        var name = folders[0];
        if (!IsValidFolderName(name))
            throw new GrandException($"'{name}' cannot be used as a plugin folder name.");

        return (name, Path.Combine(root, name));
    }

    /// <summary>
    ///     Names of the first path segments of all files. A file at the top level has no plugin folder to
    ///     own it and invalidates the archive.
    /// </summary>
    private static List<string> TopLevelFolders(string root)
    {
        var names = new List<string>();
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file);
            var separator = relative.IndexOf(Path.DirectorySeparatorChar);
            if (separator < 0)
                throw new GrandException("The archive should not contain files outside of the plugin folder.");

            var name = relative[..separator];
            if (!names.Contains(name, StringComparer.OrdinalIgnoreCase))
                names.Add(name);
        }

        return names;
    }

    private static bool IsValidFolderName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name is "." or "..")
            return false;
        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return false;
        return !IsReservedFolder(name);
    }

    private static bool IsReservedFolder(string name) =>
        string.Equals(name, BinFolderName, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, CommonPath.TmpUploadPath, StringComparison.OrdinalIgnoreCase);

    private static PluginManifest ReadSingleManifest(string folder)
    {
        var manifests = new List<PluginManifest>();
        foreach (var dll in Directory.GetFiles(folder, "*.dll", SearchOption.TopDirectoryOnly))
        {
            PluginManifest manifest;
            try
            {
                manifest = PluginManifestReader.Read(dll);
            }
            catch (Exception ex) when (ex is IOException or BadImageFormatException or UnauthorizedAccessException)
            {
                throw new GrandException($"The file '{Path.GetFileName(dll)}' could not be read as an assembly.");
            }

            if (manifest != null)
                manifests.Add(manifest);
        }

        return manifests.Count switch {
            0 => throw new GrandException(
                "No plugin info is found. The plugin assembly must be in the root of the plugin folder."),
            1 => manifests[0] is { SystemName: { Length: > 0 } }
                ? manifests[0]
                : throw new GrandException("The plugin info does not define a system name."),
            _ => throw new GrandException("The plugin folder contains more than one plugin assembly.")
        };
    }

    private bool IsSystemNameUsedByAnotherFolder(string systemName, string folderName)
    {
        if (!Directory.Exists(pluginsPath))
            return false;

        foreach (var directory in Directory.GetDirectories(pluginsPath))
        {
            var name = Path.GetFileName(directory);
            if (IsReservedFolder(name) || string.Equals(name, folderName, StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var dll in Directory.GetFiles(directory, "*.dll", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var existing = PluginManifestReader.Read(dll);
                    if (existing != null && string.Equals(existing.SystemName, systemName,
                            StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                catch (Exception ex) when (ex is IOException or BadImageFormatException
                                               or UnauthorizedAccessException)
                {
                    //an unreadable file in another plugin folder is not the uploaded plugin's concern
                }
            }
        }

        return false;
    }

    /// <summary>
    ///     Moves the existing folder aside, moves the new one in, and moves the old one back if that fails.
    ///     A locked file makes the first move fail, before anything has changed.
    /// </summary>
    private static void Replace(string source, string target, string previous)
    {
        var hadPrevious = Directory.Exists(target);
        if (hadPrevious)
        {
            try
            {
                Directory.Move(target, previous);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new GrandException(
                    "The installed version of the plugin is in use and cannot be replaced. Stop the application and try again.");
            }
        }

        try
        {
            Directory.Move(source, target);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (hadPrevious)
            {
                if (Directory.Exists(target))
                    Directory.Delete(target, true);
                Directory.Move(previous, target);
            }

            throw new GrandException("The plugin could not be installed.");
        }
    }

    private void DeleteTempRoot(string tempRoot)
    {
        try
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            //the previous plugin version moved here can hold an assembly this process has loaded;
            //it is not loaded from here on the next start and the next upload removes it
            logger.LogWarning(ex, "The temporary upload folder {TempDirectory} could not be deleted", tempRoot);
        }
    }
}

public sealed record PluginArchiveResult(string FolderName, PluginManifest Manifest);
