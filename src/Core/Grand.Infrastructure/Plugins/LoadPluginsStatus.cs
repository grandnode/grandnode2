namespace Grand.Infrastructure.Plugins
{
    /// <summary>
    /// Represents a status to load plugins
    /// </summary>
    public enum LoadPluginsStatus
    {
        /// <summary>
        /// All 
        /// </summary>
        All = 0,
        /// <summary>
        /// Installed only
        /// </summary>
        InstalledOnly = 1,
        /// <summary>
        /// Not installed only
        /// </summary>
        NotInstalledOnly = 2
    }
}