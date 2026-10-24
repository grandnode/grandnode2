namespace Grand.Web.AdminShared.Models.FileManager;

/// <summary>Why a file manager operation was refused</summary>
public enum FileManagerError
{
    None,
    InvalidPath,
    InvalidName,
    NotFound,
    AlreadyExists,
    NotAllowedType,
    TooLarge,
    RootLocked
}

/// <summary>A folder or a file, its path relative to the root of the file manager</summary>
public record FileManagerEntry(string Name, string Path, bool IsFolder, string Url, string Kind, long Size, DateTime ModifiedUtc);

/// <summary>One step of the breadcrumb, the root first</summary>
public record FileManagerCrumb(string Name, string Path);

public record FileManagerListing(
    FileManagerError Error,
    string Path,
    IReadOnlyList<FileManagerCrumb> Crumbs,
    IReadOnlyList<FileManagerEntry> Entries)
{
    public static FileManagerListing Failed(FileManagerError error) => new(error, null, [], []);
}

public record FileManagerResult(FileManagerError Error, FileManagerEntry Entry = null)
{
    public static readonly FileManagerResult Done = new(FileManagerError.None);
}

/// <summary>A file that was not uploaded, and why</summary>
public record FileManagerRejectedFile(string Name, FileManagerError Error);

public record FileManagerUploadResult(
    FileManagerError Error,
    IReadOnlyList<FileManagerEntry> Uploaded,
    IReadOnlyList<FileManagerRejectedFile> Rejected);

/// <summary>A file to upload, read once</summary>
public record FileManagerUpload(string FileName, long Length, Func<Stream> OpenReadStream);
