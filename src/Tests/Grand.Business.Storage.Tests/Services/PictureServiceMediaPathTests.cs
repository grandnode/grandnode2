using Grand.Business.Storage.Services;
using Grand.Data;
using Grand.Domain.Media;
using Grand.Infrastructure.Caching;
using Grand.Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Storage.Tests.Services;

[TestClass]
public class PictureServiceMediaPathTests
{
    private string _media;
    private string _root;
    private PictureService _service;
    private string _webRoot;

    [TestInitialize]
    public void Init()
    {
        _root = Path.Combine(Path.GetTempPath(), "picture-media-" + Guid.NewGuid().ToString("N"));
        _media = Path.Combine(_root, "media");
        _webRoot = Path.Combine(_root, "wwwroot");
        Directory.CreateDirectory(Path.Combine(_webRoot, "assets", "images"));

        var store = new DefaultMediaFileStore(MediaFileStoreFactory.Create(_webRoot, _media, null));
        _service = new PictureService(new Mock<IRepository<Picture>>().Object,
            new Mock<ILogger<PictureService>>().Object, new Mock<IMediator>().Object, new Mock<ICacheBase>().Object,
            store, new MediaSettings(), new StorageSettings());
    }

    [TestCleanup]
    public void Cleanup()
    {
        foreach (var file in Directory.GetFiles(_root, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(_root, true);
    }

    [TestMethod]
    public async Task DeletePictureOnFileSystem_OriginalOnlyInReadOnlyWebRoot_LeavesItAndDoesNotThrow()
    {
        //Arrange - wwwroot is read-only in the container (readOnlyRootFilesystem)
        var shipped = Path.Combine(_webRoot, "assets", "images", "p1_0.jpeg");
        File.WriteAllText(shipped, "jpg");
        File.SetAttributes(shipped, FileAttributes.ReadOnly);
        //Act
        await _service.DeletePictureOnFileSystem(new Picture { Id = "p1", MimeType = "image/jpeg" });
        //Assert
        Assert.IsTrue(File.Exists(shipped));
    }

    [TestMethod]
    [Timeout(30_000)]
    public async Task GetPictureUrl_ThumbWrittenAsynchronously_SavesThumbOnVolume()
    {
        //Arrange - a large original makes the thumb write complete asynchronously, resuming on another thread,
        //while parallel requests generate the same thumb; a thread-affine lock (Mutex) used to hang them here
        var service = new PictureService(new Mock<IRepository<Picture>>().Object,
            new Mock<ILogger<PictureService>>().Object, new Mock<IMediator>().Object, new Mock<ICacheBase>().Object,
            new DefaultMediaFileStore(MediaFileStoreFactory.Create(_webRoot, _media, null)), new MediaSettings(),
            new StorageSettings { PictureStoreInDb = false });
        Directory.CreateDirectory(Path.Combine(_media, "assets", "images"));
        await File.WriteAllBytesAsync(Path.Combine(_media, "assets", "images", "p1_0.jpeg"), new byte[8 * 1024 * 1024]);

        //Act
        var urls = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
            service.GetPictureUrl(new Picture { Id = "p1", MimeType = "image/jpeg" }, 0, true, "http://localhost/"))));

        //Assert
        Assert.IsTrue(File.Exists(Path.Combine(_media, "assets", "images", "thumbs", "p1.jpeg")));
        Assert.AreEqual("http://localhost/assets/images/thumbs/p1.jpeg", urls[0]);
    }

    [TestMethod]
    public async Task DeletePictureOnFileSystem_OriginalOnVolume_IsDeleted()
    {
        //Arrange
        Directory.CreateDirectory(Path.Combine(_media, "assets", "images"));
        var onVolume = Path.Combine(_media, "assets", "images", "p1_0.jpeg");
        File.WriteAllText(onVolume, "jpg");
        //Act
        await _service.DeletePictureOnFileSystem(new Picture { Id = "p1", MimeType = "image/jpeg" });
        //Assert
        Assert.IsFalse(File.Exists(onVolume));
    }
}
