using Grand.Business.Core.Interfaces.Storage;

namespace Grand.Business.Storage.Services;

public static class MediaFileStoreFactory
{
    /// <summary>
    ///     Directories whose files are never read from wwwroot once a media path is set: thumbs are regenerated
    ///     and deleted on the volume only, so a stale copy in wwwroot would outlive a picture change
    /// </summary>
    public static readonly string[] PrimaryOnlyPaths = ["assets/images/thumbs"];

    /// <summary>
    ///     Creates the file store behind IMediaFileStore
    /// </summary>
    /// <param name="webRootPath">wwwroot physical path</param>
    /// <param name="mediaRootPath">Shared media physical path; null or empty keeps everything in wwwroot</param>
    /// <param name="directory">Optional per-installation subdirectory (the "Directory" config value)</param>
    public static IFileStore Create(string webRootPath, string mediaRootPath, string directory)
    {
        var webRootStore = new FileSystemStore(Path.Combine(webRootPath, directory ?? ""));
        if (string.IsNullOrEmpty(mediaRootPath)) return webRootStore;

        var mediaDirectory = Path.Combine(mediaRootPath, directory ?? "");
        Directory.CreateDirectory(mediaDirectory);

        return new LayeredFileStore(new FileSystemStore(mediaDirectory), webRootStore, PrimaryOnlyPaths);
    }
}
