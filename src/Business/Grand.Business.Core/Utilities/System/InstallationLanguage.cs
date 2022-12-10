namespace Grand.Business.Core.Utilities.System
{
    /// <summary>
    /// Language class for installation process
    /// </summary>
    public class InstallationLanguage
    {
        public InstallationLanguage()
        {
            Resources = new List<InstallationLocaleResource>();
        }
        public string Name { get; set; }
        public string Code { get; set; }
        public bool IsDefault { get; set; }
        public bool IsRightToLeft { get; set; }

        public List<InstallationLocaleResource> Resources { get; protected set; }
    }

    public class InstallationLocaleResource
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class InstallationCollation
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
