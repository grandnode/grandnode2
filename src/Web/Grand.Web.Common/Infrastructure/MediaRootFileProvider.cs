using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Grand.Web.Common.Infrastructure;

public static class MediaRootFileProvider
{
    /// <summary>
    ///     Serves files from the media path ahead of wwwroot, so a file written at runtime replaces the copy
    ///     that shipped with the build, and every instance sharing the volume serves the same file
    /// </summary>
    /// <param name="webHostEnvironment">Host environment whose WebRootFileProvider is layered</param>
    /// <param name="mediaRootPath">Media physical path; null or empty leaves wwwroot alone</param>
    public static void Apply(IWebHostEnvironment webHostEnvironment, string mediaRootPath)
    {
        if (string.IsNullOrEmpty(mediaRootPath)) return;

        Directory.CreateDirectory(mediaRootPath);
        webHostEnvironment.WebRootFileProvider = new CompositeFileProvider(
            new PhysicalFileProvider(mediaRootPath), webHostEnvironment.WebRootFileProvider);
    }
}
