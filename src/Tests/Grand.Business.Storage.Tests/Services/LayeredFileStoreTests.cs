using Grand.Business.Storage.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Storage.Tests.Services;

[TestClass]
public class LayeredFileStoreTests
{
    private string _media;
    private string _root;
    private LayeredFileStore _store;
    private string _webRoot;

    [TestInitialize]
    public void Init()
    {
        _root = Path.Combine(Path.GetTempPath(), "layered-" + Guid.NewGuid().ToString("N"));
        _media = Path.Combine(_root, "media");
        _webRoot = Path.Combine(_root, "wwwroot");
        Directory.CreateDirectory(_media);
        Directory.CreateDirectory(Path.Combine(_webRoot, "assets", "custom"));
        Directory.CreateDirectory(Path.Combine(_webRoot, "assets", "images", "thumbs"));
        File.WriteAllText(Path.Combine(_webRoot, "assets", "custom", "style.css"), "shipped");
        File.WriteAllText(Path.Combine(_webRoot, "assets", "images", "no-image.png"), "png");
        File.WriteAllText(Path.Combine(_webRoot, "assets", "images", "thumbs", "p1_100.jpg"), "stale");

        _store = new LayeredFileStore(new FileSystemStore(_media), new FileSystemStore(_webRoot),
            ["assets/images/thumbs"]);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Directory.Delete(_root, true);
    }

    [TestMethod]
    public async Task GetFileInfo_OnlyInFallback_ReturnsFallbackEntry()
    {
        //Act
        var result = await _store.GetFileInfo("assets/images/no-image.png");
        //Assert
        Assert.IsNotNull(result);
        Assert.StartsWith(_webRoot, result.PhysicalPath);
    }

    [TestMethod]
    public async Task GetFileInfo_InBoth_ReturnsPrimary()
    {
        //Arrange
        Directory.CreateDirectory(Path.Combine(_media, "assets", "custom"));
        File.WriteAllText(Path.Combine(_media, "assets", "custom", "style.css"), "edited");
        //Act
        var result = await _store.GetFileInfo("assets/custom/style.css");
        //Assert
        Assert.StartsWith(_media, result.PhysicalPath);
    }

    [TestMethod]
    public async Task GetFileInfo_PrimaryOnlyPath_IgnoresFallback()
    {
        //Act
        var result = await _store.GetFileInfo(Path.Combine("assets", "images", "thumbs", "p1_100.jpg"));
        //Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task ReadAllText_OnlyInFallback_ReadsFallback()
    {
        //Act
        var result = await _store.ReadAllText("assets/custom/style.css");
        //Assert
        Assert.AreEqual("shipped", result);
    }

    [TestMethod]
    public async Task WriteAllText_WritesPrimaryAndLeavesFallbackUntouched()
    {
        //Arrange
        _store.TryCreateDirectory("assets/custom");
        //Act
        await _store.WriteAllText("assets/custom/style.css", "edited");
        //Assert
        Assert.AreEqual("edited", await _store.ReadAllText("assets/custom/style.css"));
        Assert.AreEqual("shipped", File.ReadAllText(Path.Combine(_webRoot, "assets", "custom", "style.css")));
    }

    [TestMethod]
    public void GetDirectoryInfo_OnlyInFallback_CreatesItInPrimary()
    {
        //Act
        var result = _store.GetDirectoryInfo("assets/images");
        //Assert
        Assert.IsNotNull(result);
        Assert.StartsWith(_media, result.PhysicalPath);
        Assert.IsTrue(Directory.Exists(Path.Combine(_media, "assets", "images")));
    }

    [TestMethod]
    public void GetDirectoryInfo_UploadedShippedInWebRoot_FileManagerRootIsOnVolume()
    {
        //Arrange - wwwroot ships assets/images/uploaded with a placeholder.txt; elFinder takes PhysicalPath as its root
        Directory.CreateDirectory(Path.Combine(_webRoot, "assets", "images", "uploaded"));
        File.WriteAllText(Path.Combine(_webRoot, "assets", "images", "uploaded", "legacy.jpg"), "old");
        //Act
        var result = _store.GetDirectoryInfo(Path.Combine("assets", "images", "uploaded"));
        //Assert
        Assert.AreEqual(Path.Combine(_media, "assets", "images", "uploaded"), result.PhysicalPath);
        Assert.AreEqual("assets/images/uploaded", result.Path.Replace("\\", "/"));
    }

    [TestMethod]
    public void GetDirectoryInfo_MissingEverywhere_ReturnsNull()
    {
        //Act
        var result = _store.GetDirectoryInfo("assets/nothing");
        //Assert
        Assert.IsNull(result);
        Assert.IsFalse(Directory.Exists(Path.Combine(_media, "assets", "nothing")));
    }

    [TestMethod]
    public void GetDirectoryContent_MergesLayersWithPrimaryWinning()
    {
        //Arrange
        Directory.CreateDirectory(Path.Combine(_media, "assets", "custom"));
        File.WriteAllText(Path.Combine(_media, "assets", "custom", "style.css"), "edited");
        File.WriteAllText(Path.Combine(_media, "assets", "custom", "script.js"), "js");
        //Act
        var result = _store.GetDirectoryContent("assets/custom");
        //Assert
        Assert.HasCount(2, result);
        Assert.IsTrue(result.All(e => e.PhysicalPath.StartsWith(_media)));
    }

    [TestMethod]
    public async Task GetFileInfo_PathOutsideRoot_Throws()
    {
        //Act + Assert
        await Assert.ThrowsExactlyAsync<Exception>(() => _store.GetFileInfo("../outside.txt"));
    }
}
