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

    /// <summary>Limit applied to the real bytes written while extracting; lowered by tests only.</summary>
    internal long UncompressedBytesLimit { get; init; } = MaxUncompressedBytes;

    /// <summary>Directory move; replaced by tests to simulate failures.</summary>
    internal Action<string, string> MoveDirectory { get; init; } = Directory.Move;

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
        var keepPrevious = false;
        try
        {
            CheckArchiveSize(archivePath);

            var extracted = Path.Combine(tempRoot, ExtractedFolderName);
            Extract(archivePath, extracted);

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
        catch (RollbackFailedException)
        {
            keepPrevious = true;
            throw;
        }
        finally
        {
            //after a failed rollback the previous version is only in the temp folder: leave it there
            DeleteTempRoot(keepPrevious ? Path.Combine(tempRoot, ExtractedFolderName) : tempRoot);
        }
    }

    private void CheckArchiveSize(string archivePath)
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
                if (total > UncompressedBytesLimit)
                    throw new GrandException(
                        $"The archive is too large when unpacked (the limit is {UncompressedBytesLimit / 1024 / 1024} MB).");
            }
        }
        catch (InvalidDataException)
        {
            throw new GrandException("The file is not a valid zip archive.");
        }
    }

    /// <summary>
    ///     Extracts entry by entry so the real number of bytes written is bounded, whatever the headers claim,
    ///     and so every destination is checked against the extraction root. Backslashes in entry names are
    ///     path separators on every platform.
    /// </summary>
    private void Extract(string archivePath, string destination)
    {
        Directory.CreateDirectory(destination);
        var root = Path.GetFullPath(destination) + Path.DirectorySeparatorChar;
        long written = 0;
        try
        {
            using var archive = ZipFile.OpenRead(archivePath);
            foreach (var entry in archive.Entries)
            {
                var name = entry.FullName.Replace('\\', '/');
                var full = Path.GetFullPath(Path.Combine(destination, name));
                if (!full.StartsWith(root, StringComparison.Ordinal))
                    throw new GrandException("The archive contains a path outside of the plugin folder.");

                if (name.EndsWith('/'))
                {
                    Directory.CreateDirectory(full);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                using var input = entry.Open();
                using var output = new FileStream(full, FileMode.Create, FileAccess.Write, FileShare.None);
                var buffer = new byte[81920];
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    written += read;
                    if (written > UncompressedBytesLimit)
                        throw new GrandException(
                            $"The archive is too large when unpacked (the limit is {UncompressedBytesLimit / 1024 / 1024} MB).");
                    output.Write(buffer, 0, read);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            throw new GrandException("The archive could not be unpacked. Check that it is a valid plugin archive.");
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
    private void Replace(string source, string target, string previous)
    {
        var hadPrevious = Directory.Exists(target);
        if (hadPrevious)
        {
            try
            {
                MoveDirectory(target, previous);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new GrandException(
                    "The installed version of the plugin is in use and cannot be replaced. Stop the application and try again.");
            }
        }

        try
        {
            MoveDirectory(source, target);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (hadPrevious)
            {
                try
                {
                    if (Directory.Exists(target))
                        Directory.Delete(target, true);
                    MoveDirectory(previous, target);
                }
                catch (Exception rollback)
                {
                    logger.LogError(rollback,
                        "The plugin could not be installed and the previous version could not be restored; it is kept in {PreviousPath}",
                        previous);
                    throw new RollbackFailedException(
                        $"The plugin could not be installed and the previous version could not be restored. The previous version is preserved at '{previous}'.");
                }
            }

            throw new GrandException("The plugin could not be installed.");
        }
    }

    private sealed class RollbackFailedException(string message) : GrandException(message);

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
