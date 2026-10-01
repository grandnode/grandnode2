using Grand.Web.Common.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.Infrastructure;

[TestClass]
public class MediaRootFileProviderTests
{
    private Mock<IWebHostEnvironment> _env;
    private string _media;
    private string _root;
    private string _webRoot;

    [TestInitialize]
    public void Init()
    {
        _root = Path.Combine(Path.GetTempPath(), "mediaroot-" + Guid.NewGuid().ToString("N"));
        _webRoot = Path.Combine(_root, "wwwroot");
        _media = Path.Combine(_root, "media");
        Directory.CreateDirectory(Path.Combine(_webRoot, "assets", "custom"));
        File.WriteAllText(Path.Combine(_webRoot, "assets", "custom", "style.css"), "shipped");
        File.WriteAllText(Path.Combine(_webRoot, "favicon.ico"), "ico");

        _env = new Mock<IWebHostEnvironment>();
        _env.SetupProperty(e => e.WebRootFileProvider, new PhysicalFileProvider(_webRoot));
    }

    [TestCleanup]
    public void Cleanup()
    {
        Directory.Delete(_root, true);
    }

    private static string Read(IFileProvider provider, string path)
    {
        using var reader = new StreamReader(provider.GetFileInfo(path).CreateReadStream());
        return reader.ReadToEnd();
    }

    [TestMethod]
    public void Apply_NoMediaPath_LeavesProviderUntouched()
    {
        //Arrange
        var original = _env.Object.WebRootFileProvider;
        //Act
        MediaRootFileProvider.Apply(_env.Object, null);
        //Assert
        Assert.AreSame(original, _env.Object.WebRootFileProvider);
    }

    [TestMethod]
    public void Apply_MissingMediaDirectory_IsCreated()
    {
        //Act
        MediaRootFileProvider.Apply(_env.Object, _media);
        //Assert
        Assert.IsTrue(Directory.Exists(_media));
    }

    [TestMethod]
    public void Apply_FileInBoth_MediaWins()
    {
        //Arrange
        Directory.CreateDirectory(Path.Combine(_media, "assets", "custom"));
        File.WriteAllText(Path.Combine(_media, "assets", "custom", "style.css"), "edited");
        //Act
        MediaRootFileProvider.Apply(_env.Object, _media);
        //Assert
        Assert.AreEqual("edited", Read(_env.Object.WebRootFileProvider, "assets/custom/style.css"));
    }

    [TestMethod]
    public void Apply_FileOnlyInWebRoot_StillServed()
    {
        //Act
        MediaRootFileProvider.Apply(_env.Object, _media);
        //Assert
        Assert.AreEqual("ico", Read(_env.Object.WebRootFileProvider, "favicon.ico"));
    }

    [TestMethod]
    public void Apply_SitemapOnlyInMedia_ServedAtRoot()
    {
        //Arrange
        Directory.CreateDirectory(_media);
        File.WriteAllText(Path.Combine(_media, "sitemap.xml"), "<urlset/>");
        //Act
        MediaRootFileProvider.Apply(_env.Object, _media);
        //Assert
        Assert.AreEqual("<urlset/>", Read(_env.Object.WebRootFileProvider, "sitemap.xml"));
    }
}
