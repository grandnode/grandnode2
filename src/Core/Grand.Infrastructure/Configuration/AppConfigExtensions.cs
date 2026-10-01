namespace Grand.Infrastructure.Configuration;

public static class AppConfigExtensions
{
    /// <summary>
    ///     Resolves <see cref="AppConfig.MediaPath" /> to a full physical path
    /// </summary>
    /// <param name="appConfig">Application config</param>
    /// <param name="contentRootPath">Content root a relative media path is resolved against</param>
    /// <returns>The full path, or null when no media path is configured</returns>
    public static string GetMediaRootPath(this AppConfig appConfig, string contentRootPath)
    {
        ArgumentNullException.ThrowIfNull(appConfig);

        return string.IsNullOrWhiteSpace(appConfig.MediaPath)
            ? null
            : Path.GetFullPath(appConfig.MediaPath, contentRootPath);
    }
}
