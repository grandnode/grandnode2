using System.Reflection;
using System.Text.RegularExpressions;

namespace Grand.Infrastructure;

public static class GrandVersion
{
    /// <summary>
    ///     Gets the major version
    /// </summary>
    public static readonly string MajorVersion = Assembly.GetExecutingAssembly().GetName().Version?.Major.ToString();

    /// <summary>
    ///     Gets the minor version
    /// </summary>
    public static readonly string MinorVersion = Assembly.GetExecutingAssembly().GetName().Version?.Minor.ToString();

    /// <summary>
    ///     Gets the full version
    /// </summary>
    public static readonly string FullVersion = $"{MajorVersion}.{MinorVersion}.{PatchVersion}";

    /// <summary>
    ///     Gets the Supported DB version
    /// </summary>
    public static readonly string SupportedDBVersion = $"{MajorVersion}.{MinorVersion}";

    /// <summary>
    ///     Gets the Supported plugin version
    /// </summary>
    public static readonly string SupportedPluginVersion = $"{MajorVersion}.{MinorVersion}";

    /// <summary>
    ///     Gets the git branch
    /// </summary>
    public static string GitBranch = GetGitBranch();

    /// <summary>
    ///     Gets the git commit
    /// </summary>
    public static string GitCommit = GetGitHash();

    /// <summary>
    ///     Gets the patch version
    /// </summary>
    public static string PatchVersion {
        get {
            var assembly = Assembly.GetExecutingAssembly();
            if (assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute)) is not
                AssemblyInformationalVersionAttribute infoVersionAttribute) return "0";
            var fullVersion = infoVersionAttribute.InformationalVersion;
            var match = Regex.Match(fullVersion, @"(\d+)\.(\d+)\.(\d+)(?:-([^\+]+))?");
            if (!match.Success) return "0";
            var patch = match.Groups[3].Value;
            var suffix = match.Groups[4].Value;
            return string.IsNullOrEmpty(suffix) ? patch : $"{patch}-{suffix}";
        }
    }

    private static string _gitHash { get; set; }

    private static string _gitBranch { get; set; }

    private static string GetGitHash()
    {
        if (!string.IsNullOrEmpty(_gitHash)) return _gitHash;

        var version = "1.0.0+LOCALBUILD";
        var appAssembly = typeof(GrandVersion).Assembly;
        var infoVerAttr = (AssemblyInformationalVersionAttribute)appAssembly
            .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute)).FirstOrDefault();
        if (infoVerAttr is { InformationalVersion.Length: > 6 }) version = infoVerAttr.InformationalVersion;
        _gitHash = version[(version.IndexOf('+') + 1)..];
        return _gitHash;
    }

    private static string GetGitBranch()
    {
        if (!string.IsNullOrEmpty(_gitBranch)) return _gitBranch;

        var appAssembly = typeof(GrandVersion).Assembly;
        var infoBranchAttr = appAssembly.GetCustomAttributes(typeof(AssemblyMetadataAttribute))
            .FirstOrDefault(x => ((AssemblyMetadataAttribute)x).Key == "GitBranch");
        _gitBranch = infoBranchAttr != null ? ((AssemblyMetadataAttribute)infoBranchAttr).Value : "UNKNOWN";
        return _gitBranch;
    }
}