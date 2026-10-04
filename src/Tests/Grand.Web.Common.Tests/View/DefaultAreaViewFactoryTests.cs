using Grand.Web.Common.Themes;
using Grand.Web.Common.View;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.View;

/// <summary>
///     A store whose DefaultStoreTheme names a theme that is not installed (a sample install made
///     Nordic the default, then the plugin was removed) must still render: the storefront falls
///     back to the Default views instead of looking in folders nobody registered.
/// </summary>
[TestClass]
public class DefaultAreaViewFactoryTests
{
    private static readonly string[] DefaultLocations = { "/Views/{1}/{0}.cshtml", "/Views/Shared/{0}.cshtml" };

    private static DefaultAreaViewFactory Factory(string currentTheme, params IThemeView[] themes)
    {
        var themeContext = new Mock<IThemeContext>();
        themeContext.Setup(x => x.GetCurrentTheme()).Returns(currentTheme);
        var contextFactory = new Mock<IThemeContextFactory>();
        contextFactory.Setup(x => x.GetThemeContext("")).Returns(themeContext.Object);
        return new DefaultAreaViewFactory(themes, contextFactory.Object);
    }

    private static IThemeView Theme(string name, string area = "")
    {
        var theme = new Mock<IThemeView>();
        theme.Setup(x => x.ThemeName).Returns(name);
        theme.Setup(x => x.AreaName).Returns(area);
        theme.Setup(x => x.GetViewLocations()).Returns(new[] { $"/Views/{name}/{{1}}/{{0}}.cshtml", $"/Views/{name}/Shared/{{0}}.cshtml" });
        return theme.Object;
    }

    [TestMethod]
    public void GetViewLocations_ThemeNotRegistered_FallsBackToDefaultViews()
    {
        var factory = Factory("Nordic", Theme("Modern"));

        CollectionAssert.AreEqual(DefaultLocations, factory.GetViewLocations(Array.Empty<string>()).ToArray());
    }

    [TestMethod]
    public void GetViewLocations_ThemeRegisteredForAnotherArea_FallsBackToDefaultViews()
    {
        var factory = Factory("Nordic", Theme("Nordic", "Admin"));

        CollectionAssert.AreEqual(DefaultLocations, factory.GetViewLocations(Array.Empty<string>()).ToArray());
    }

    [TestMethod]
    public void GetViewLocations_ThemeRegistered_PutsThemeFirstThenDefaultViews()
    {
        var factory = Factory("Nordic", Theme("Modern"), Theme("Nordic"));

        CollectionAssert.AreEqual(new[] {
            "/Views/Nordic/{1}/{0}.cshtml",
            "/Views/Nordic/Shared/{0}.cshtml",
            "/Views/{1}/{0}.cshtml",
            "/Views/Shared/{0}.cshtml"
        }, factory.GetViewLocations(Array.Empty<string>()).ToArray());
    }

    [TestMethod]
    public void GetViewLocations_NoThemeSet_UsesDefaultViews()
    {
        var factory = Factory("", Theme("Nordic"));

        CollectionAssert.AreEqual(DefaultLocations, factory.GetViewLocations(Array.Empty<string>()).ToArray());
    }
}
