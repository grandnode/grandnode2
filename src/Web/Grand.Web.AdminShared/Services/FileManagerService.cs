using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Media;
using Grand.SharedKernel.Extensions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.FileManager;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;

namespace Grand.Web.AdminShared.Services;

public class FileManagerService : IFileManagerService
{
    /// <summary>The root of the file manager, relative to the media store</summary>
    public const string RootPath = "assets/images/uploaded";

    /// <summary>The largest file accepted, as elFinder's volume did before</summary>
    public const long MaxUploadBytes = 4 * 1024 * 1024;

    /// <summary>The largest upload request, several files at once</summary>
    public const long MaxUploadRequestBytes = 8 * MaxUploadBytes;

    private const int MaxNameLength = 255;

    //the characters Windows refuses in a name, checked on every platform so a name valid here is valid on any host
    private static readonly char[] InvalidNameChars = ['/', '\\', ':', '*', '?', '"', '<', '>', '|'];

    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase) {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    private readonly IMediaFileStore _mediaFileStore;
    private readonly MediaSettings _mediaSettings;
    private readonly string _urlPrefix;

    public FileManagerService(IMediaFileStore mediaFileStore, MediaSettings mediaSettings, IConfiguration configuration)
    {
        _mediaFileStore = mediaFileStore;
        _mediaSettings = mediaSettings;
        var directory = configuration[CommonPath.DirectoryParam];
        _urlPrefix = string.IsNullOrEmpty(directory) ? "/" : $"/{directory.Trim('/')}/";
    }

    public virtual FileManagerListing List(string path)
    {
        if (!TryGetSegments(path, out var segments))
            return FileManagerListing.Failed(FileManagerError.InvalidPath);

        EnsureRoot();
        var storePath = StorePath(segments);
        if (_mediaFileStore.GetDirectoryInfo(storePath) is not { IsDirectory: true })
            return FileManagerListing.Failed(FileManagerError.NotFound);

        var allowed = AllowedTypes();
        var entries = _mediaFileStore.GetDirectoryContent(storePath)
            .Where(e => !e.Name.StartsWith('.'))
            .Where(e => e.IsDirectory || allowed.IsAllowedMediaFileType(Path.GetExtension(e.Name)))
            .Select(e => ToEntry(segments.Append(e.Name).ToArray(), e))
            .OrderByDescending(e => e.IsFolder)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var crumbs = new List<FileManagerCrumb> { new("", "") };
        for (var i = 0; i < segments.Length; i++)
            crumbs.Add(new FileManagerCrumb(segments[i], string.Join('/', segments[..(i + 1)])));

        return new FileManagerListing(FileManagerError.None, string.Join('/', segments), crumbs, entries);
    }

    public virtual async Task<FileManagerUploadResult> Upload(string path, IReadOnlyList<FileManagerUpload> files)
    {
        if (!TryGetSegments(path, out var segments))
            return new FileManagerUploadResult(FileManagerError.InvalidPath, [], []);

        EnsureRoot();
        if (_mediaFileStore.GetDirectoryInfo(StorePath(segments)) is not { IsDirectory: true })
            return new FileManagerUploadResult(FileManagerError.NotFound, [], []);

        var allowed = AllowedTypes();
        var uploaded = new List<FileManagerEntry>();
        var rejected = new List<FileManagerRejectedFile>();
        foreach (var file in files ?? [])
        {
            //browsers send a bare name, older ones the whole client path
            var name = Path.GetFileName((file.FileName ?? "").Replace('\\', '/'));
            var error = !IsValidName(name) ? FileManagerError.InvalidName
                : !allowed.IsAllowedMediaFileType(Path.GetExtension(name)) ? FileManagerError.NotAllowedType
                : file.Length > MaxUploadBytes ? FileManagerError.TooLarge
                : FileManagerError.None;
            if (error != FileManagerError.None)
            {
                rejected.Add(new FileManagerRejectedFile(name, error));
                continue;
            }

            //an upload never replaces a file: a taken name gets a number
            var target = await FreeName(segments, name);
            var targetSegments = segments.Append(target).ToArray();
            await using (var stream = file.OpenReadStream())
            {
                await _mediaFileStore.CreateFileFromStream(StorePath(targetSegments), stream);
            }

            uploaded.Add(ToEntry(targetSegments, await _mediaFileStore.GetFileInfo(StorePath(targetSegments))));
        }

        return new FileManagerUploadResult(FileManagerError.None, uploaded, rejected);
    }

    public virtual async Task<FileManagerResult> CreateFolder(string path, string name)
    {
        if (!TryGetSegments(path, out var segments))
            return new FileManagerResult(FileManagerError.InvalidPath);
        name = name?.Trim();
        if (!IsValidName(name))
            return new FileManagerResult(FileManagerError.InvalidName);

        EnsureRoot();
        if (_mediaFileStore.GetDirectoryInfo(StorePath(segments)) is not { IsDirectory: true })
            return new FileManagerResult(FileManagerError.NotFound);

        var target = segments.Append(name).ToArray();
        if (await Exists(target))
            return new FileManagerResult(FileManagerError.AlreadyExists);

        _mediaFileStore.TryCreateDirectory(StorePath(target));
        return new FileManagerResult(FileManagerError.None, ToEntry(target, _mediaFileStore.GetDirectoryInfo(StorePath(target))));
    }

    public virtual async Task<FileManagerResult> Rename(string path, string name)
    {
        if (!TryGetSegments(path, out var segments))
            return new FileManagerResult(FileManagerError.InvalidPath);
        if (segments.Length == 0)
            return new FileManagerResult(FileManagerError.RootLocked);
        name = name?.Trim();
        if (!IsValidName(name))
            return new FileManagerResult(FileManagerError.InvalidName);

        var storePath = StorePath(segments);
        var folder = _mediaFileStore.GetDirectoryInfo(storePath);
        var file = folder == null ? await _mediaFileStore.GetFileInfo(storePath) : null;
        if (folder == null && file == null)
            return new FileManagerResult(FileManagerError.NotFound);

        //a file keeps an allowed type: renaming must not turn a picture into a script the site serves
        if (file != null && !AllowedTypes().IsAllowedMediaFileType(Path.GetExtension(name)))
            return new FileManagerResult(FileManagerError.NotAllowedType);

        var target = segments[..^1].Append(name).ToArray();
        if (string.Equals(segments[^1], name, StringComparison.Ordinal))
            return new FileManagerResult(FileManagerError.None, ToEntry(target, folder ?? file));
        //a change of case only is a rename of the same entry; a case-insensitive file system sees the new name as taken,
        //so that rename goes through a temporary name
        var caseOnly = string.Equals(segments[^1], name, StringComparison.OrdinalIgnoreCase);
        if (!caseOnly && await Exists(target))
            return new FileManagerResult(FileManagerError.AlreadyExists);

        if (caseOnly)
        {
            var temporary = $"{Guid.NewGuid():N}{Path.GetExtension(name)}";
            await RenameEntry(folder != null, storePath, temporary);
            storePath = StorePath(segments[..^1].Append(temporary));
        }

        await RenameEntry(folder != null, storePath, name);
        return new FileManagerResult(FileManagerError.None, folder != null
            ? ToEntry(target, _mediaFileStore.GetDirectoryInfo(StorePath(target)))
            : ToEntry(target, await _mediaFileStore.GetFileInfo(StorePath(target))));
    }

    private Task RenameEntry(bool isFolder, string storePath, string name)
    {
        return isFolder ? _mediaFileStore.TryRenameDirectory(storePath, name) : _mediaFileStore.RenameFile(storePath, name);
    }

    public virtual async Task<FileManagerResult> Delete(string path)
    {
        if (!TryGetSegments(path, out var segments))
            return new FileManagerResult(FileManagerError.InvalidPath);
        if (segments.Length == 0)
            return new FileManagerResult(FileManagerError.RootLocked);

        var storePath = StorePath(segments);
        if (_mediaFileStore.GetDirectoryInfo(storePath) != null)
            return await _mediaFileStore.TryDeleteDirectory(storePath)
                ? FileManagerResult.Done
                : new FileManagerResult(FileManagerError.NotFound);

        return await _mediaFileStore.TryDeleteFile(storePath)
            ? FileManagerResult.Done
            : new FileManagerResult(FileManagerError.NotFound);
    }

    /// <summary>
    ///     Splits a path relative to the root into names, each one checked. Anything that could step out of the root -
    ///     "..", ".", an empty segment, a drive or a reserved name - fails, so the result always lies under the root.
    /// </summary>
    public static bool TryGetSegments(string path, out string[] segments)
    {
        segments = [];
        if (string.IsNullOrEmpty(path))
            return true;

        var parts = path.Replace('\\', '/').Trim('/').Split('/');
        if (parts.Length == 1 && parts[0] == "")
            return true;
        if (!parts.All(IsValidName))
            return false;

        segments = parts;
        return true;
    }

    /// <summary>A single file or folder name the store can hold on any platform and the site can serve</summary>
    public static bool IsValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > MaxNameLength)
            return false;
        //".", ".." and hidden files; Windows drops a trailing dot or space, so the stored name would differ
        if (name.StartsWith('.') || name.EndsWith('.') || name.EndsWith(' ') || name.StartsWith(' '))
            return false;
        if (name.IndexOfAny(InvalidNameChars) >= 0 || name.Any(char.IsControl))
            return false;

        return !ReservedNames.Contains(Path.GetFileNameWithoutExtension(name));
    }

    protected virtual IList<string> AllowedTypes()
    {
        return FileExtensions.GetAllowedMediaFileTypes(_mediaSettings.AllowedFileTypes);
    }

    private static string StorePath(IEnumerable<string> segments)
    {
        return string.Join('/', new[] { RootPath }.Concat(segments));
    }

    private void EnsureRoot()
    {
        if (_mediaFileStore.GetDirectoryInfo(RootPath) == null)
            _mediaFileStore.TryCreateDirectory(RootPath);
    }

    private async Task<bool> Exists(string[] segments)
    {
        var storePath = StorePath(segments);
        return _mediaFileStore.GetDirectoryInfo(storePath) != null || await _mediaFileStore.GetFileInfo(storePath) != null;
    }

    private async Task<string> FreeName(string[] folder, string name)
    {
        var baseName = Path.GetFileNameWithoutExtension(name);
        var extension = Path.GetExtension(name);
        var candidate = name;
        for (var i = 1; await Exists(folder.Append(candidate).ToArray()); i++)
            candidate = $"{baseName} ({i}){extension}";
        return candidate;
    }

    private FileManagerEntry ToEntry(string[] segments, IFileStoreEntry entry)
    {
        var relative = string.Join('/', segments);
        if (entry.IsDirectory)
            return new FileManagerEntry(entry.Name, relative, true, null, "folder", 0, entry.LastModifiedUtc);

        var url = _urlPrefix + string.Join('/', StorePath(segments).Split('/').Select(Uri.EscapeDataString));
        return new FileManagerEntry(entry.Name, relative, false, url, Kind(entry.Name), entry.Length, entry.LastModifiedUtc);
    }

    private static string Kind(string name)
    {
        if (!ContentTypes.TryGetContentType(name, out var contentType))
            return "file";
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return "image";
        return contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ? "video" : "file";
    }
}
