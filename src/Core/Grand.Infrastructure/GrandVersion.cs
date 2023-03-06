namespace Grand.Infrastructure
{
    public static class GrandVersion
    {

        /// <summary>
        /// Gets the major version
        /// </summary>
        public const string MajorVersion = "2";

        /// <summary>
        /// Gets the minor version
        /// </summary>
        public const string MinorVersion = "1";

        /// <summary>
        /// Gets the patch version
        /// </summary>
        public const string PatchVersion = "0-develop";

        /// <summary>
        /// Gets the full version
        /// </summary>
        public const string FullVersion = $"{MajorVersion}.{MinorVersion}.{PatchVersion}";

        /// <summary>
        /// Gets the Supported DB version
        /// </summary>
        public const string SupportedDBVersion = $"{MajorVersion}.{MinorVersion}";

        /// <summary>
        /// Gets the Supported plugin version
        /// </summary>
        public const string SupportedPluginVersion = $"{MajorVersion}.{MinorVersion}";

        /// <summary>
        /// Gets the git branch
        /// </summary>
        public const string GitBranch = ThisAssembly.Git.Branch;
        
        /// <summary>
        /// Gets the git commit
        /// </summary>
        public const string GitCommit = ThisAssembly.Git.Commit;
    }
}
