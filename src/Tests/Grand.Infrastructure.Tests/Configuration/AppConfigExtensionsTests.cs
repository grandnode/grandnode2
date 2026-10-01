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
    [DataRow(".")]
    [DataRow("..")]
    [DataRow("App_Data")]
    [DataRow("app_data/")]
    public void GetMediaRootPath_PathThatWouldPublishApplicationFiles_Throws(string mediaPath)
    {
        //Arrange - the media root is served at "/", so it must not expose appsettings.json or Settings.cfg
        var config = new AppConfig { MediaPath = mediaPath };
        //Act + Assert
        Assert.ThrowsExactly<InvalidOperationException>(() => config.GetMediaRootPath(ContentRoot));
    }

    [TestMethod]
    public void GetMediaRootPath_FileSystemRoot_Throws()
    {
        //Arrange - a root keeps its trailing separator, which must not be doubled into a non-matching prefix
        var config = new AppConfig { MediaPath = Path.GetPathRoot(ContentRoot) };
        //Act + Assert
        Assert.ThrowsExactly<InvalidOperationException>(() => config.GetMediaRootPath(ContentRoot));
    }

    [TestMethod]
    public void GetMediaRootPath_SubdirectoryOfAppData_Allowed()
    {
        //Arrange - a media folder inside a shared App_Data volume serves only that folder
        var config = new AppConfig { MediaPath = Path.Combine("App_Data", "media") };
        //Act
        var result = config.GetMediaRootPath(ContentRoot);
        //Assert
        Assert.AreEqual(Path.Combine(ContentRoot, "App_Data", "media"), result);
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
