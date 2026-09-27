using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Theme.Nordic;

namespace Grand.Modules.Tests.Themes;

/// <summary>
///     A theme is selectable without installing its plugin, so the Nordic labels must read as
///     English rather than as resource keys when the plugin's resources were never installed.
/// </summary>
[TestClass]
public class NordicTextTests
{
    private const string Key = "Theme.Nordic.Badge.Sale";

    private static IStringLocalizer Localizer(string value, bool notFound)
    {
        var loc = new Mock<IStringLocalizer>();
        loc.Setup(x => x[Key]).Returns(new LocalizedString(Key, value, notFound));
        return loc.Object;
    }

    [TestMethod]
    public void Get_ResourceInstalled_ReturnsTranslation()
    {
        Assert.AreEqual("Wyprzedaż", NordicText.Get(Localizer("Wyprzedaż", false), Key));
    }

    [TestMethod]
    public void Get_TranslationServiceEchoesLowerCasedKey_ReturnsEnglishDefault()
    {
        Assert.AreEqual("Sale", NordicText.Get(Localizer(Key.ToLowerInvariant(), false), Key));
    }

    [TestMethod]
    public void Get_ResourceNotFound_ReturnsEnglishDefault()
    {
        Assert.AreEqual("Sale", NordicText.Get(Localizer(Key, true), Key));
    }

    [TestMethod]
    public void Get_EmptyValue_ReturnsEnglishDefault()
    {
        Assert.AreEqual("Sale", NordicText.Get(Localizer("", false), Key));
    }
}
