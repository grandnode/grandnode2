using Microsoft.VisualStudio.TestTools.UnitTesting;
using Theme.Nordic;

namespace Grand.Modules.Tests.Themes;

[TestClass]
public class NordicThemeViewTests
{
    private readonly NordicThemeView _view = new();

    [TestMethod]
    public void ThemeName_MatchesViewFolder()
    {
        Assert.AreEqual("Nordic", _view.ThemeName);
        Assert.IsTrue(Directory.Exists(Path.Combine(Installer.RepositoryPaths.Root, "src", "Plugins", "Theme.Nordic", "Views", _view.ThemeName)));
    }

    [TestMethod]
    public void ViewLocations_ThemeFirst_DefaultFallbacksLast()
    {
        CollectionAssert.AreEqual(new[] {
            "/Views/Nordic/{1}/{0}.cshtml",
            "/Views/Nordic/Shared/{0}.cshtml",
            "/Views/{1}/{0}.cshtml",
            "/Views/Shared/{0}.cshtml"
        }, _view.GetViewLocations().ToArray());
    }

    [TestMethod]
    public void ThemeInfo_PreviewImageExists_StorefrontArea()
    {
        Assert.AreEqual("", _view.AreaName);
        Assert.AreEqual("~/Plugins/Theme.Nordic/Content/theme.jpg", _view.ThemeInfo.PreviewImageUrl);
        Assert.IsTrue(File.Exists(Path.Combine(Installer.RepositoryPaths.Root, "src", "Plugins", "Theme.Nordic", "Content", "theme.jpg")));
        Assert.IsFalse(_view.ThemeInfo.SupportRtl);
    }
}
