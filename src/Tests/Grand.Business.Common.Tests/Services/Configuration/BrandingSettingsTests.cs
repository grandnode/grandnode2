using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Domain.Stores;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Common.Tests.Services.Configuration;

[TestClass]
public class BrandingSettingsTests
{
    [TestMethod]
    public void BrandingSettings_DefaultInstance_HasNullColorProperties()
    {
        var settings = new BrandingSettings();
        Assert.IsNull(settings.PrimaryColor);
        Assert.IsNull(settings.SecondaryColor);
        Assert.IsNull(settings.AccentColor);
        Assert.IsNull(settings.BackgroundColor);
        Assert.IsNull(settings.TextColor);
    }

    [TestMethod]
    public void BrandingSettings_DefaultInstance_HasNullPictureIds()
    {
        var settings = new BrandingSettings();
        Assert.IsNull(settings.LogoPictureId);
        Assert.IsNull(settings.FaviconPictureId);
        Assert.IsNull(settings.BannerPictureId);
    }

    [TestMethod]
    public void BrandingSettings_ImplementsISettings()
    {
        var settings = new BrandingSettings();
        Assert.IsInstanceOfType(settings, typeof(ISettings));
    }
}
