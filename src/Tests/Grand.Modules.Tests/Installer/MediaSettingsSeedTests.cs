using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Installer;

/// <summary>
///     New installations encode thumbnails at JPEG quality 80, not 100. Read from the installer
///     source, like the other seed guards in this folder.
/// </summary>
[TestClass]
public class MediaSettingsSeedTests
{
    [TestMethod]
    public void InstallSettings_SeedsImageQuality80()
    {
        var source = File.ReadAllText(Path.Combine(RepositoryPaths.InstallerServices, "InstallDataSettings.cs"));
        var seed = Regex.Match(source, @"new MediaSettings\s*\{(?<body>.*?)\}\);", RegexOptions.Singleline);

        Assert.IsTrue(seed.Success, "MediaSettings seed not found in InstallDataSettings.cs");
        StringAssert.Matches(seed.Groups["body"].Value, new Regex(@"ImageQuality\s*=\s*80\b"));
    }
}
