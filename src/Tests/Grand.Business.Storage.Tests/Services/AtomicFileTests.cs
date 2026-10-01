using Grand.Business.Storage.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Storage.Tests.Services;

[TestClass]
public class AtomicFileTests
{
    private string _dir;

    [TestInitialize]
    public void Init()
    {
        _dir = Path.Combine(Path.GetTempPath(), "atomicfile-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Directory.Delete(_dir, true);
    }

    [TestMethod]
    public void WriteAllBytes_NewFile_WritesContentAndLeavesNoTempFile()
    {
        //Arrange
        var path = Path.Combine(_dir, "thumb.jpg");
        //Act
        AtomicFile.WriteAllBytes(path, [1, 2, 3]);
        //Assert
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, File.ReadAllBytes(path));
        Assert.HasCount(1, Directory.GetFiles(_dir));
    }

    [TestMethod]
    public async Task WriteAllTextAsync_ExistingFile_OverwritesAndLeavesNoTempFile()
    {
        //Arrange
        var path = Path.Combine(_dir, "sitemap.xml");
        await File.WriteAllTextAsync(path, "old content that is longer than the new one");
        //Act
        await AtomicFile.WriteAllTextAsync(path, "new");
        //Assert
        Assert.AreEqual("new", await File.ReadAllTextAsync(path));
        Assert.HasCount(1, Directory.GetFiles(_dir));
    }

    [TestMethod]
    public async Task WriteAllBytes_TargetHeldOpenForReading_ReplacesOnceReaderCloses()
    {
        //Arrange - the static file middleware opens files with FileShare.ReadWrite
        var path = Path.Combine(_dir, "sitemap.xml");
        File.WriteAllBytes(path, [9]);
        var reader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var closeReader = Task.Delay(200).ContinueWith(_ => reader.Dispose());

        //Act
        AtomicFile.WriteAllBytes(path, [1, 2]);
        await closeReader;

        //Assert
        CollectionAssert.AreEqual(new byte[] { 1, 2 }, File.ReadAllBytes(path));
        Assert.HasCount(1, Directory.GetFiles(_dir));
    }
}
