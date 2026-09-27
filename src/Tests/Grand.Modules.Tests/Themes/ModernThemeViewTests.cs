using Microsoft.VisualStudio.TestTools.UnitTesting;
using Theme.Modern;

namespace Grand.Modules.Tests.Themes;

[TestClass]
public class ModernThemeViewTests
{
    private readonly ModernThemeView _view = new();

    [TestMethod]
    public void ThemeName_MatchesViewFolder()
    {
        Assert.AreEqual("Modern", _view.ThemeName);
        Assert.IsTrue(Directory.Exists(Path.Combine(Installer.RepositoryPaths.Root, "src", "Plugins", "Theme.Modern", "Views", _view.ThemeName)));
    }

    [TestMethod]
    public void ViewLocations_ThemeFirst_DefaultFallbacksLast()
    {
        CollectionAssert.AreEqual(new[] {
            "/Views/Modern/{1}/{0}.cshtml",
            "/Views/Modern/Shared/{0}.cshtml",
            "/Views/{1}/{0}.cshtml",
            "/Views/Shared/{0}.cshtml"
        }, _view.GetViewLocations().ToArray());
    }

    [TestMethod]
    public void ThemeInfo_IsNoLongerBeta_PreviewImageExists()
    {
        Assert.AreEqual("", _view.AreaName);
        Assert.AreEqual("Modern", _view.ThemeInfo.Title);
        Assert.IsFalse(_view.ThemeInfo.PreviewText.Contains("beta", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual("~/Plugins/Theme.Modern/Content/theme.jpg", _view.ThemeInfo.PreviewImageUrl);
        Assert.IsTrue(File.Exists(Path.Combine(Installer.RepositoryPaths.Root, "src", "Plugins", "Theme.Modern", "Content", "theme.jpg")));
        Assert.IsFalse(_view.ThemeInfo.SupportRtl);
    }
}
