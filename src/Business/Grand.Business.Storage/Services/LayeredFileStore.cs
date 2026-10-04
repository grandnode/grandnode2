using Grand.Business.Core.Interfaces.Storage;
using Microsoft.Extensions.FileProviders.Physical;

namespace Grand.Business.Storage.Services;

/// <summary>
///     A writable primary store over a read-only fallback. Files written at runtime go to the primary
///     (a volume shared by every instance); files that ship with the build are still found in the fallback (wwwroot).
/// </summary>
public class LayeredFileStore : IFileStore
{
    private readonly IFileStore _fallback;
    private readonly IFileStore _primary;
    private readonly string[] _primaryOnlyPaths;

    /// <param name="primary">Writable store</param>
    /// <param name="fallback">Read-only store consulted when a file is missing in the primary</param>
    /// <param name="primaryOnlyPaths">
    ///     Directories whose files are never read from the fallback (generated files a stale copy
    ///     would shadow)
    /// </param>
    public LayeredFileStore(IFileStore primary, IFileStore fallback, IEnumerable<string> primaryOnlyPaths)
    {
        _primary = primary;
        _fallback = fallback;
        _primaryOnlyPaths = primaryOnlyPaths.Select(p => this.NormalizePath(p)).ToArray();
    }

    public async Task<IFileStoreEntry> GetFileInfo(string path)
    {
        return await _primary.GetFileInfo(path)
               ?? (IsPrimaryOnly(path) ? null : await _fallback.GetFileInfo(path));
    }

    public IFileStoreEntry GetDirectoryInfo(string path)
    {
        var entry = _primary.GetDirectoryInfo(path);
        if (entry != null) return entry;

        if (_fallback.GetDirectoryInfo(path) == null) return null;

        //the directory ships with the build but is not on the volume yet: create it, so writers land on the volume
        _primary.TryCreateDirectory(path);
        return _primary.GetDirectoryInfo(path);
    }

    public Task<PhysicalDirectoryInfo> GetPhysicalDirectoryInfo(string directoryPath)
    {
        return GetDirectoryInfo(directoryPath) == null
            ? Task.FromResult<PhysicalDirectoryInfo>(null)
            : _primary.GetPhysicalDirectoryInfo(directoryPath);
    }

    public IList<IFileStoreEntry> GetDirectoryContent(string path = null, bool includeSubDirectories = false,
        bool listDirectories = true, bool listFiles = true)
    {
        var primaryEntries = _primary.GetDirectoryContent(path, includeSubDirectories, listDirectories, listFiles);
        if (IsPrimaryOnly(path)) return primaryEntries;

        var primaryPaths = primaryEntries.Select(e => e.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return primaryEntries
            .Concat(_fallback.GetDirectoryContent(path, includeSubDirectories, listDirectories, listFiles)
                .Where(e => !primaryPaths.Contains(e.Path)))
            .ToList();
    }

    public bool TryCreateDirectory(string path) => _primary.TryCreateDirectory(path);

    public Task<bool> TryRenameDirectory(string path, string newName) => _primary.TryRenameDirectory(path, newName);

    public Task<bool> TryDeleteFile(string path) => _primary.TryDeleteFile(path);

    public Task<bool> TryDeleteDirectory(string path) => _primary.TryDeleteDirectory(path);

    public Task MoveFile(string oldPath, string newPath) => _primary.MoveFile(oldPath, newPath);

    public Task CopyFile(string srcPath, string dstPath) => _primary.CopyFile(srcPath, dstPath);

    public Task RenameFile(string file, string newName) => _primary.RenameFile(file, newName);

    public async Task<Stream> GetFileStream(string path) => await (await ReadSource(path)).GetFileStream(path);

    public Task<Stream> GetFileStream(IFileStoreEntry fileStoreEntry) => GetFileStream(fileStoreEntry.Path);

    public Task<string> CreateFileFromStream(string path, Stream inputStream, bool overwrite = false)
    {
        return _primary.CreateFileFromStream(path, inputStream, overwrite);
    }

    public async Task<string> ReadAllText(string path) => await (await ReadSource(path)).ReadAllText(path);

    public Task WriteAllText(string path, string text) => _primary.WriteAllText(path, text);

    private async Task<IFileStore> ReadSource(string path)
    {
        return IsPrimaryOnly(path) || await _primary.GetFileInfo(path) != null ? _primary : _fallback;
    }

    private bool IsPrimaryOnly(string path)
    {
        var normalized = this.NormalizePath(path) ?? "";
        return _primaryOnlyPaths.Any(p =>
            normalized.Equals(p, StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith(p + "/", StringComparison.OrdinalIgnoreCase));
    }
}
