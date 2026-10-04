using Grand.Business.Storage.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Grand.Web.Common.Infrastructure;

public static class MediaRootFileProvider
{
    /// <summary>
    ///     Serves files from the media path ahead of wwwroot, so a file written at runtime replaces the copy
    ///     that shipped with the build, and every instance sharing the volume serves the same file
    /// </summary>
    /// <param name="webHostEnvironment">Host environment whose WebRootFileProvider is layered</param>
    /// <param name="mediaRootPath">Media physical path; null or empty leaves wwwroot alone</param>
    /// <param name="directory">Optional per-installation subdirectory (the "Directory" config value)</param>
    public static void Apply(IWebHostEnvironment webHostEnvironment, string mediaRootPath, string directory)
    {
        if (string.IsNullOrEmpty(mediaRootPath)) return;

        //created once here, so the per-request media file store never has to
        Directory.CreateDirectory(Path.Combine(mediaRootPath, directory ?? ""));

        //thumbs never come from wwwroot, as in the media file store: a stale copy would outlive a deleted picture
        var hiddenPaths = MediaFileStoreFactory.PrimaryOnlyPaths
            .Select(p => string.IsNullOrEmpty(directory) ? p : $"{directory.Trim('/', '\\')}/{p}")
            .ToArray();

        webHostEnvironment.WebRootFileProvider = new CompositeFileProvider(
            new PhysicalFileProvider(mediaRootPath),
            new HidingFileProvider(webHostEnvironment.WebRootFileProvider, hiddenPaths));
    }

    private sealed class HidingFileProvider(IFileProvider inner, string[] hiddenPaths) : IFileProvider
    {
        public IFileInfo GetFileInfo(string subpath)
        {
            return IsHidden(subpath) ? new NotFoundFileInfo(subpath) : inner.GetFileInfo(subpath);
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return IsHidden(subpath) ? NotFoundDirectoryContents.Singleton : inner.GetDirectoryContents(subpath);
        }

        public IChangeToken Watch(string filter) => inner.Watch(filter);

        private bool IsHidden(string subpath)
        {
            var path = (subpath ?? "").Replace('\\', '/').Trim('/');
            return hiddenPaths.Any(p =>
                path.Equals(p, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(p + "/", StringComparison.OrdinalIgnoreCase));
        }
    }
}
