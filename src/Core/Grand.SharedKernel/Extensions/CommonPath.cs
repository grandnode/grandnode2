namespace Grand.SharedKernel.Extensions;

public static class CommonPath
{
    public static string AppData => "App_Data";

    /// <summary>
    ///     Extra parameter to path
    /// </summary>
    public static string Param { get; set; } = "";

    /// <summary>
    ///     Maps a settings path to a physical disk path.
    /// </summary>
    /// <returns>The physical path.</returns>
    public static string DataProtectionKeysPath => Path.Combine(BaseDirectory, AppData, Param, "DataProtectionKeys");

    /// <summary>
    ///     Maps a settings path to a physical disk path.
    /// </summary>
    /// <returns>The physical path.</returns>
    public static string SettingsPath => Path.Combine(BaseDirectory, AppData, Param, "Settings.cfg");

    /// <summary>
    ///     Maps a theme path to a physical disk path.
    /// </summary>
    /// <returns>The physical path.</returns>
    public static string PluginsPath => Path.Combine(BaseDirectory, "Plugins");

    /// <summary>
    ///     Maps a image path.
    /// </summary>
    /// <returns>The path.</returns>
    public static string ImagePath => Path.Combine("assets", "images");

    /// <summary>
    ///     Maps a image thumb path.
    /// </summary>
    /// <returns>The path.</returns>
    public static string ImageThumbPath => Path.Combine("assets", "images", "thumbs");

    /// <summary>
    ///     Maps a image upload path.
    /// </summary>
    /// <returns>The path.</returns>
    public static string ImageUploadedPath => Path.Combine("assets", "images", "uploaded");


    /// <summary>
    ///     Maps a installled plugins path to a physical disk path.
    /// </summary>
    /// <returns>The physical path.</returns>
    public static string InstalledPluginsFilePath =>
        Path.Combine(BaseDirectory, AppData, Param, "InstalledPlugins.cfg");

    /// <summary>
    ///     Maps a installled plugins path to a physical disk path.
    /// </summary>
    /// <returns>The physical path.</returns>
    public static string PluginsCopyPath => Path.Combine(BaseDirectory, PluginsPath, "bin");

    /// <summary>
    ///     Maps a temp upload path to a physical disk path.
    /// </summary>
    /// <returns>The physical path.</returns>
    public static string TmpUploadPath => Path.Combine(BaseDirectory, AppData, Param, "TempUploads");

    /// <summary>
    ///     Gets or sets application base path
    /// </summary>
    public static string BaseDirectory { get; set; }

    /// <summary>
    ///     Gets or sets web application content files
    /// </summary>
    public static string WebRootPath => Path.Combine(WebHostEnvironment, Param);

    /// <summary>
    ///     Gets or sets web application content files
    /// </summary>
    public static string WebHostEnvironment { get; set; }

    /// <summary>
    ///     Maps a path to a physical disk path.
    /// </summary>
    /// <param name="path">The path to map. E.g. "~/bin"</param>
    /// <returns>The physical path. E.g. "c:\inetpub\wwwroot\bin"</returns>
    public static string MapPath(string path)
    {
        path = path.Replace("~/", "").TrimStart('/');
        return Path.Combine(BaseDirectory, path);
    }

    /// <summary>
    ///     Maps a virtual path to a physical disk path (for tenants).
    /// </summary>
    /// <param name="path">The path to map. E.g. "~/bin"</param>
    /// <returns>The physical path. E.g. "c:\inetpub\wwwroot\"</returns>
    public static string WebMapPath(string path)
    {
        path = path.Replace("~/", "").TrimStart('/');
        return Path.Combine(WebRootPath, path);
    }

    /// <summary>
    ///     Maps a virtual path to a physical disk path. (not for tenants)
    /// </summary>
    /// <param name="path">The path to map. E.g. "~/bin"</param>
    /// <returns>The physical path. E.g. "c:\inetpub\wwwroot\"</returns>
    public static string WebHostMapPath(string path)
    {
        path = path.Replace("~/", "").TrimStart('/');
        return Path.Combine(WebHostEnvironment, path);
    }
}