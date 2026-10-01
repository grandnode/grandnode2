using Grand.SharedKernel.Extensions;

namespace Grand.Infrastructure.Configuration;

public static class AppConfigExtensions
{
    /// <summary>
    ///     Resolves <see cref="AppConfig.MediaPath" /> to a full physical path
    /// </summary>
    /// <param name="appConfig">Application config</param>
    /// <param name="contentRootPath">Content root a relative media path is resolved against</param>
    /// <returns>The full path, or null when no media path is configured</returns>
    /// <exception cref="InvalidOperationException">
    ///     The media path is the content root, one of its parents, or App_Data: the media root is served
    ///     at "/", so it would publish appsettings.json and Settings.cfg
    /// </exception>
    public static string GetMediaRootPath(this AppConfig appConfig, string contentRootPath)
    {
        ArgumentNullException.ThrowIfNull(appConfig);

        if (string.IsNullOrWhiteSpace(appConfig.MediaPath)) return null;

        var mediaRoot = Path.GetFullPath(appConfig.MediaPath, contentRootPath);
        var contentRoot = Path.GetFullPath(contentRootPath);

        if (IsSameOrParentOf(mediaRoot, contentRoot) ||
            IsSame(mediaRoot, Path.Combine(contentRoot, CommonPath.AppData)))
            throw new InvalidOperationException(
                $"Application:MediaPath '{appConfig.MediaPath}' would publish application files; use a dedicated directory, e.g. App_Data/media.");

        return mediaRoot;
    }

    private static bool IsSame(string path, string other)
    {
        return string.Equals(Path.TrimEndingDirectorySeparator(path), Path.TrimEndingDirectorySeparator(other),
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSameOrParentOf(string parent, string child)
    {
        var parentWithSeparator = Path.TrimEndingDirectorySeparator(parent) + Path.DirectorySeparatorChar;
        return IsSame(parent, child) ||
               child.StartsWith(parentWithSeparator, StringComparison.OrdinalIgnoreCase);
    }
}
