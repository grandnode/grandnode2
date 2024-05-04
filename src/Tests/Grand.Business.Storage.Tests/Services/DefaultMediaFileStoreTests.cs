using Grand.Business.Storage.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestContext = NUnit.Framework.TestContext;

namespace Grand.Business.Storage.Tests.Services;

[TestClass]
public class DefaultMediaFileStoreTests
{
    private readonly string myFile = "sample.txt";
    private DefaultMediaFileStore _defaultMediaFileStore;

    [TestInitialize]
    public void Init()
    {
        _defaultMediaFileStore =
            new DefaultMediaFileStore(
                new FileSystemStore(TestContext.CurrentContext.TestDirectory));
    }

    [TestMethod]
    public async Task GetFileInfoTest()
    {
        //Act
        var result = await _defaultMediaFileStore.GetFileInfo(myFile);
        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(result.Name, myFile);
    }

    [TestMethod]
    public void GetDirectoryInfoTest()
    {
        //Act
        var result = _defaultMediaFileStore.GetDirectoryInfo("Upload");
        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(result.Name, "Upload");
    }

    [TestMethod]
    public async Task GetPhysicalDirectoryInfoTest()
    {
        //Act
        var result = await _defaultMediaFileStore.GetPhysicalDirectoryInfo("Upload");
        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(result.Name, "Upload");
        Assert.IsTrue(result.IsDirectory);
    }

    [TestMethod]
    public void GetDirectoryContentTest()
    {
        //Act
        var result = _defaultMediaFileStore.GetDirectoryContent("Upload");
        //Assert
        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public void TryCreateDirectoryTest()
    {
        //Act
        var result = _defaultMediaFileStore.TryCreateDirectory("Test");
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    [Ignore] //TMP
    public async Task TryRenameDirectoryTest()
    {
        //Arrange
        _defaultMediaFileStore.TryCreateDirectory("Test");
        await _defaultMediaFileStore.TryDeleteDirectory("Test2");
        //Act
        var result = await _defaultMediaFileStore.TryRenameDirectory("Test", "Test2");
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task TryDeleteFileTest()
    {
        //Arrange 
        await _defaultMediaFileStore.WriteAllText("file078.txt", "test");
        //Act
        var result = await _defaultMediaFileStore.TryDeleteFile("file078.txt");
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task TryDeleteDirectoryTest()
    {
        //Arrange
        _defaultMediaFileStore.TryCreateDirectory("Test");

        //Act
        var result = await _defaultMediaFileStore.TryDeleteDirectory("Test");
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task MoveFileTest()
    {
        //Arrange 
        await _defaultMediaFileStore.WriteAllText("file1998.txt", "test");
        await _defaultMediaFileStore.TryDeleteFile("file2197.txt");

        //Act
        await _defaultMediaFileStore.MoveFile("file1998.txt", "file2197.txt");
        var fileStoreEntry = await _defaultMediaFileStore.GetFileInfo("file2197.txt");

        //Assert
        Assert.IsNotNull(fileStoreEntry);
        Assert.AreEqual("file2197.txt", fileStoreEntry.Name);
    }

    [TestMethod]
    public async Task CopyFileTest()
    {
        //Arrange 
        await _defaultMediaFileStore.WriteAllText("file178.txt", "test");
        await _defaultMediaFileStore.TryDeleteFile("file297.txt");
        //Act
        await _defaultMediaFileStore.CopyFile("file178.txt", "file297.txt");
        var fileStoreEntry = await _defaultMediaFileStore.GetFileInfo("file297.txt");

        //Assert
        Assert.IsNotNull(fileStoreEntry);
        Assert.AreEqual("file297.txt", fileStoreEntry.Name);
    }

    [TestMethod]
    public async Task RenameFileTest()
    {
        //Arrange 
        await _defaultMediaFileStore.WriteAllText("file678.txt", "test");
        await _defaultMediaFileStore.TryDeleteFile("file897.txt");
        //Act
        await _defaultMediaFileStore.RenameFile("file678.txt", "file897.txt");
        var fileStoreEntry = await _defaultMediaFileStore.GetFileInfo("file897.txt");

        //Assert
        Assert.IsNotNull(fileStoreEntry);
        Assert.AreEqual("file897.txt", fileStoreEntry.Name);
    }

    [TestMethod]
    public async Task GetFileStreamTest()
    {
        //Arrange
        var fileStoreEntry = await _defaultMediaFileStore.GetFileInfo(myFile);
        //Act
        var stream = await _defaultMediaFileStore.GetFileStream(fileStoreEntry);
        //Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var text = reader.ReadToEnd();
        Assert.AreEqual("test", text);
    }

    [TestMethod]
    public async Task GetFileStreamTest1()
    {
        //Arrange
        var result = await _defaultMediaFileStore.CreateFileFromStream("file456.txt",
            new MemoryStream(Encoding.UTF8.GetBytes("test")), true);
        //Act
        var stream = await _defaultMediaFileStore.GetFileStream("file456.txt");
        //Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var text = reader.ReadToEnd();
        Assert.AreEqual("test", text);
    }

    [TestMethod]
    public async Task CreateFileFromStreamTest()
    {
        //Act
        var result = await _defaultMediaFileStore.CreateFileFromStream("file456.txt",
            new MemoryStream(Encoding.UTF8.GetBytes("test")), true);
        //Assert
        Assert.AreEqual("file456.txt", result);
    }

    [TestMethod]
    public async Task ReadAllTextTest()
    {
        await _defaultMediaFileStore.WriteAllText("file123.txt", "test");
        //Act
        var result = await _defaultMediaFileStore.ReadAllText("file123.txt");
        //Assert
        Assert.AreEqual("test", result);
    }

    [TestMethod]
    public async Task WriteAllTextTest()
    {
        //Act
        await _defaultMediaFileStore.WriteAllText("file111.txt", "test");

        //Assert
        var result = await _defaultMediaFileStore.ReadAllText("file111.txt");
        Assert.AreEqual("test", result);
    }
}