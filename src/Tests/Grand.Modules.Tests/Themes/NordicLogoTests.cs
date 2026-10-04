using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Themes;

/// <summary>
///     The spec's header centre is "the store name as a serif wordmark (or logo)": a merchant who
///     uploaded a logo keeps it under Nordic; the wordmark is for stores without one.
/// </summary>
[TestClass]
public class NordicLogoTests
{
    private static string Logo => File.ReadAllText(Path.Combine(Installer.RepositoryPaths.Root,
        "src", "Plugins", "Theme.Nordic", "Views", "Nordic", "Shared", "Partials", "Logo.cshtml"));

    [TestMethod]
    public void Logo_RendersTheMerchantPictureWhenOneIsSet()
    {
        StringAssert.Contains(Logo, "LogoPictureId");
        StringAssert.Contains(Logo, "<img");
    }

    [TestMethod]
    public void Logo_FallsBackToTheWordmark()
    {
        StringAssert.Contains(Logo, "nordic-wordmark");
        StringAssert.Contains(Logo, "CurrentStore.Name");
    }
}
