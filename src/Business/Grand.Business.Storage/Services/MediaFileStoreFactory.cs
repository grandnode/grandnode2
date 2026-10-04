using Grand.Business.Core.Interfaces.Storage;

namespace Grand.Business.Storage.Services;

public static class MediaFileStoreFactory
{
    /// <summary>
    ///     Directories whose files are never read from wwwroot once a media path is set: thumbs are regenerated
    ///     and deleted on the volume only, so a stale copy in wwwroot would outlive a picture change
    /// </summary>
    public static readonly IReadOnlyList<string> PrimaryOnlyPaths = Array.AsReadOnly(new[] { "assets/images/thumbs" });

    /// <summary>
    ///     Creates the file store behind IMediaFileStore
    /// </summary>
    /// <param name="webRootPath">wwwroot physical path</param>
    /// <param name="mediaRootPath">Shared media physical path; null or empty keeps everything in wwwroot</param>
    /// <param name="directory">Optional per-installation subdirectory (the "Directory" config value)</param>
    /// <remarks>
    ///     Runs once per request scope, so it does not touch the file system; the media directory is created
    ///     at startup (MediaRootFileProvider.Apply)
    /// </remarks>
    public static IFileStore Create(string webRootPath, string mediaRootPath, string directory)
    {
        var webRootStore = new FileSystemStore(Path.Combine(webRootPath, directory ?? ""));
        if (string.IsNullOrEmpty(mediaRootPath)) return webRootStore;

        var mediaStore = new FileSystemStore(Path.Combine(mediaRootPath, directory ?? ""));
        return new LayeredFileStore(mediaStore, webRootStore, PrimaryOnlyPaths);
    }
}
