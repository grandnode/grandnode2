using Grand.Business.Storage.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Storage.Tests.Services;

[TestClass]
public class MediaFileStoreFactoryTests
{
    private string _root;
    private string _webRoot;

    [TestInitialize]
    public void Init()
    {
        _root = Path.Combine(Path.GetTempPath(), "mediafactory-" + Guid.NewGuid().ToString("N"));
        _webRoot = Path.Combine(_root, "wwwroot");
        Directory.CreateDirectory(Path.Combine(_webRoot, "tenant"));
    }

    [TestCleanup]
    public void Cleanup()
    {
        Directory.Delete(_root, true);
    }

    [TestMethod]
    public void Create_NoMediaPath_ReturnsPlainWebRootStore()
    {
        //Act
        var result = MediaFileStoreFactory.Create(_webRoot, null, null);
        //Assert
        Assert.IsInstanceOfType<FileSystemStore>(result);
    }

    [TestMethod]
    public async Task Create_WithMediaPathAndDirectory_WritesUnderMediaDirectory()
    {
        //Arrange
        var media = Path.Combine(_root, "media");
        Directory.CreateDirectory(Path.Combine(media, "tenant"));
        var store = MediaFileStoreFactory.Create(_webRoot, media, "tenant");
        //Act
        await store.WriteAllText("sitemap.xml", "<urlset/>");
        //Assert
        Assert.IsTrue(File.Exists(Path.Combine(media, "tenant", "sitemap.xml")));
        Assert.IsFalse(File.Exists(Path.Combine(_webRoot, "tenant", "sitemap.xml")));
    }

    [TestMethod]
    public void Create_WithMediaPath_DoesNotTouchTheFileSystem()
    {
        //Arrange - resolved once per request scope; the media directory is created at startup instead
        var media = Path.Combine(_root, "media");
        //Act
        MediaFileStoreFactory.Create(_webRoot, media, "tenant");
        //Assert
        Assert.IsFalse(Directory.Exists(media));
    }
}
