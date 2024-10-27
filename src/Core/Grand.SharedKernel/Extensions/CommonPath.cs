namespace Grand.SharedKernel.Extensions;

public static class CommonPath
{
    public static string AppData => "App_Data";

    public static string Plugins => "Plugins";

    public static string TmpUploadPath = "TempUploads";

    public static string DirectoryParam = "Directory";

    public static string SettingsFile = "Settings.cfg";

    public static string InstalledPluginsFile = "InstalledPlugins.cfg";

    /// <summary>
    ///     Extra parameter to path
    /// </summary>
    public static string Param { get; set; } = "";
    
    /// <summary>
    ///     Gets or sets application base path
    /// </summary>
    public static string BaseDirectory { get; set; }

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