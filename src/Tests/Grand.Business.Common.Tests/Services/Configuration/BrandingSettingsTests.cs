using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Data;
using Grand.Domain.Configuration;
using Grand.Domain.Stores;
using Grand.Infrastructure.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Common.Tests.Services.Configuration;

[TestClass]
public class BrandingSettingsTests
{
    private Mock<ICacheBase> _cacheMock;
    private Mock<IRepository<Setting>> _repositoryMock;

    [TestInitialize]
    public void Init()
    {
        _cacheMock = new Mock<ICacheBase>();
        _repositoryMock = new Mock<IRepository<Setting>>();
        _cacheMock
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<IList<Setting>>>>()))
            .ReturnsAsync(new List<Setting>());
    }

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
