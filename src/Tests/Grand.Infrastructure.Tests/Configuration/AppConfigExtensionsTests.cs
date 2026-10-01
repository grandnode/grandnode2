using Grand.Infrastructure.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.Configuration;

[TestClass]
public class AppConfigExtensionsTests
{
    private static readonly string ContentRoot = Path.Combine(Path.GetTempPath(), "grand-content-root");

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void GetMediaRootPath_NotConfigured_ReturnsNull(string mediaPath)
    {
        //Arrange
        var config = new AppConfig { MediaPath = mediaPath };
        //Act
        var result = config.GetMediaRootPath(ContentRoot);
        //Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetMediaRootPath_RelativePath_ResolvesAgainstContentRoot()
    {
        //Arrange
        var config = new AppConfig { MediaPath = "media" };
        //Act
        var result = config.GetMediaRootPath(ContentRoot);
        //Assert
        Assert.AreEqual(Path.Combine(ContentRoot, "media"), result);
    }

    [TestMethod]
    public void GetMediaRootPath_AbsolutePath_ReturnedAsIs()
    {
        //Arrange
        var absolute = Path.Combine(Path.GetTempPath(), "shared-media");
        var config = new AppConfig { MediaPath = absolute };
        //Act
        var result = config.GetMediaRootPath(ContentRoot);
        //Assert
        Assert.AreEqual(absolute, result);
    }
}
