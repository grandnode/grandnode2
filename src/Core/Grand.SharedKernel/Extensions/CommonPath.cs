using System.IO;

namespace Grand.SharedKernel.Extensions
{
    public static class CommonPath
    {
        private static string AppData => "App_Data";
        /// <summary>
        /// Extra parameter to path
        /// </summary>
        public static string Param { get; set; } = "";

        /// <summary>
        /// Maps a path to a physical disk path.
        /// </summary>
        /// <param name="path">The path to map. E.g. "~/bin"</param>
        /// <returns>The physical path. E.g. "c:\inetpub\wwwroot\bin"</returns>
        public static string MapPath(string path)
        {
            path = path.Replace("~/", "").TrimStart('/');
            return Path.Combine(BaseDirectory, path);
        }

        /// <summary>
        /// Maps a settings path to a physical disk path.
        /// </summary>
        /// <returns>The physical path.</returns>
        public static string DataProtectionKeysPath
        {
            get
            {
                return Path.Combine(BaseDirectory, AppData, Param, "DataProtectionKeys");
            }
        }

        /// <summary>
        /// Maps a settings path to a physical disk path.
        /// </summary>
        /// <returns>The physical path.</returns>
        public static string SettingsPath
        {
            get
            {
                return Path.Combine(BaseDirectory, AppData, Param, "Settings.cfg");
            }
        }

        /// <summary>
        /// Maps a theme path to a physical disk path.
        /// </summary>
        /// <returns>The physical path.</returns>
        public static string ThemePath
        {
            get
            {
                return Path.Combine(BaseDirectory, "Themes");
            }
        }

        /// <summary>
        /// Maps a theme path to a physical disk path.
        /// </summary>
        /// <returns>The physical path.</returns>
        public static string PluginsPath
        {
            get
            {
                return Path.Combine(BaseDirectory, "Plugins");
            }
        }

        /// <summary>
        /// Maps a installled plugins path to a physical disk path.
        /// </summary>
        /// <returns>The physical path.</returns>
        public static string InstalledPluginsFilePath
        {
            get
            {
                return Path.Combine(BaseDirectory, AppData, Param, "InstalledPlugins.cfg");
            }
        }
        /// <summary>
        /// Maps a installled plugins path to a physical disk path.
        /// </summary>
        /// <returns>The physical path.</returns>
        public static string PluginsCopyPath
        {
            get
            {
                return Path.Combine(BaseDirectory, PluginsPath, "bin");
            }
        }
        /// <summary>
        /// Maps a temp upload path to a physical disk path.
        /// </summary>
        /// <returns>The physical path.</returns>
        public static string TmpUploadPath
        {
            get
            {
                return Path.Combine(BaseDirectory, AppData, Param, "TempUploads");
            }
        }
        /// <summary>
        /// Gets or sets application base path
        /// </summary>
        public static string BaseDirectory { get; set; }

        /// <summary>
        /// Maps a virtual path to a physical disk path.
        /// </summary>
        /// <param name="path">The path to map. E.g. "~/bin"</param>
        /// <returns>The physical path. E.g. "c:\inetpub\wwwroot\bin"</returns>
        public static string WebMapPath(string path)
        {
            path = path.Replace("~/", "").TrimStart('/');
            return Path.Combine(WebRootPath, path);
        }

        /// <summary>
        /// Gets or sets web application content files
        /// </summary>
        public static string WebRootPath 
        { 
            get 
            {
                return Path.Combine(WebHostEnvironment, Param);
            } 
        }

        /// <summary>
        /// Gets or sets web application content files
        /// </summary>
        public static string WebHostEnvironment { get; set; }

    }
}
