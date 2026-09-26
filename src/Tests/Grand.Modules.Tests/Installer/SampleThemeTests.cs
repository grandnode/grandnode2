using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Installer;

/// <summary>
///     A sample-data install opens in the Nordic theme with a header promo line; a plain install
///     stays on Default with no promo page. Read from the installer sources, like the other
///     sample-data guards in this folder.
/// </summary>
[TestClass]
public class SampleThemeTests
{
    private static string Read(string file) => File.ReadAllText(Path.Combine(RepositoryPaths.InstallerServices, file));

    [TestMethod]
    public void DefaultStoreTheme_IsNordicOnlyForSampleInstalls()
    {
        StringAssert.Contains(Read("InstallDataSettings.cs"), "DefaultStoreTheme = installSampleData ? \"Nordic\" : \"Default\"");
    }

    [TestMethod]
    public void HeaderPromoTextPage_IsAddedOnlyForSampleInstalls()
    {
        var source = Read("InstallDataPages.cs");
        Assert.IsTrue(Regex.IsMatch(source,
                @"if\s*\(\s*installSampleData\s*\)\s*(?:\{\s*)?pages\.Add\(\s*new\s+Page\s*\{\s*SystemName\s*=\s*""HeaderPromoText""",
                RegexOptions.Singleline),
            "HeaderPromoText must be added inside an if (installSampleData) block");
        Assert.AreEqual(1, Regex.Matches(source, @"""HeaderPromoText""").Count, "HeaderPromoText must be seeded once");
    }
}
