using Grand.Business.Storage.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestContext = NUnit.Framework.TestContext;

namespace Grand.Business.Storage.Tests.Services;

[TestClass]
public class FileSystemStoreTests
{
    private readonly string myFile = "sample.txt";
    private FileSystemStore _store;

    [TestInitialize]
    public void Init()
    {
        _store = new FileSystemStore(TestContext.CurrentContext.TestDirectory);
    }


    [TestMethod]
    public async Task GetFileInfoTest()
    {
        //Act
        var result = await _store.GetFileInfo(myFile);
        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(myFile, result.Name);
    }

    [TestMethod]
    public void GetDirectoryInfoTest()
    {
        //Act
        var result = _store.GetDirectoryInfo("Upload");
        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Upload", result.Name);
    }

    [TestMethod]
    public async Task GetPhysicalDirectoryInfoTest()
    {
        //Act
        var result = await _store.GetPhysicalDirectoryInfo("Upload");
        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Upload", result.Name);
        Assert.IsTrue(result.IsDirectory);
    }

    [TestMethod]
    public void GetDirectoryContentTest()
    {
        //Act
        var result = _store.GetDirectoryContent("Upload");
        //Assert
        Assert.HasCount(1, result);
        Assert.IsNotEmpty(result);
    }

    [TestMethod]
    public void TryCreateDirectoryTest()
    {
        //Act
        var result = _store.TryCreateDirectory("Test");
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task TryRenameDirectoryTest()
    {
        //Arrange
        await _store.TryDeleteDirectory("RenameTest");
        await _store.TryDeleteDirectory("RenameTest2");
        _store.TryCreateDirectory("RenameTest");
        //Act
        var result = await _store.TryRenameDirectory("RenameTest", "RenameTest2");
        //Assert
        Assert.IsTrue(result);
        Assert.IsNotNull(_store.GetDirectoryInfo("RenameTest2"));
        Assert.IsNull(_store.GetDirectoryInfo("RenameTest"));
    }

    [TestMethod]
    public async Task TryRenameDirectory_Nested_StaysInItsParent()
    {
        //Arrange
        await _store.TryDeleteDirectory("RenameNested");
        _store.TryCreateDirectory("RenameNested/a/old");
        //Act
        var result = await _store.TryRenameDirectory("RenameNested/a/old", "new");
        //Assert
        Assert.IsTrue(result);
        Assert.IsNotNull(_store.GetDirectoryInfo("RenameNested/a/new"));
        await _store.TryDeleteDirectory("RenameNested");
    }

    [TestMethod]
    [DataRow("../moved")]
    [DataRow("a/b")]
    [DataRow("a\\b")]
    [DataRow("..")]
    public async Task Rename_NewNameWithAPath_Throws(string newName)
    {
        //Arrange
        await _store.TryDeleteDirectory("RenameGuard");
        _store.TryCreateDirectory("RenameGuard/folder");
        await _store.WriteAllText("RenameGuard/file.txt", "test");
        //Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _store.TryRenameDirectory("RenameGuard/folder", newName));
        await Assert.ThrowsAsync<ArgumentException>(() => _store.RenameFile("RenameGuard/file.txt", newName));
        await _store.TryDeleteDirectory("RenameGuard");
    }

    [TestMethod]
    public void GetDirectoryInfo_SiblingSharingTheRootPrefix_Throws()
    {
        //Arrange - "<root>-old" starts with the root's path but lies outside it
        var root = Path.Combine(Path.GetTempPath(), $"store-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(root + "-old");
        try
        {
            var store = new FileSystemStore(root);
            //Act & Assert
            Assert.ThrowsExactly<Exception>(() => store.GetDirectoryInfo($"../{Path.GetFileName(root)}-old"));
            Assert.IsNotNull(store.GetDirectoryInfo(""));
        }
        finally
        {
            Directory.Delete(root, true);
            Directory.Delete(root + "-old", true);
        }
    }

    [TestMethod]
    public async Task TryDeleteFileTest()
    {
        //Arrange 
        await _store.WriteAllText("file078.txt", "test");
        //Act
        var result = await _store.TryDeleteFile("file078.txt");
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task TryDeleteDirectoryTest()
    {
        //Arrange
        _store.TryCreateDirectory("Test");

        //Act
        var result = await _store.TryDeleteDirectory("Test");
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task MoveFileTest()
    {
        //Arrange 
        await _store.WriteAllText("file1998.txt", "test");
        await _store.TryDeleteFile("file2197.txt");

        //Act
        await _store.MoveFile("file1998.txt", "file2197.txt");
        var fileStoreEntry = await _store.GetFileInfo("file2197.txt");

        //Assert
        Assert.IsNotNull(fileStoreEntry);
        Assert.AreEqual("file2197.txt", fileStoreEntry.Name);
    }

    [TestMethod]
    public async Task CopyFileTest()
    {
        //Arrange 
        await _store.WriteAllText("file178.txt", "test");
        await _store.TryDeleteFile("file297.txt");
        //Act
        await _store.CopyFile("file178.txt", "file297.txt");
        var fileStoreEntry = await _store.GetFileInfo("file297.txt");

        //Assert
        Assert.IsNotNull(fileStoreEntry);
        Assert.AreEqual("file297.txt", fileStoreEntry.Name);
    }

    [TestMethod]
    public async Task RenameFileTest()
    {
        //Arrange 
        await _store.WriteAllText("file678.txt", "test");
        await _store.TryDeleteFile("file897.txt");
        //Act
        await _store.RenameFile("file678.txt", "file897.txt");
        var fileStoreEntry = await _store.GetFileInfo("file897.txt");

        //Assert
        Assert.IsNotNull(fileStoreEntry);
        Assert.AreEqual("file897.txt", fileStoreEntry.Name);
    }


    [TestMethod]
    public async Task GetFileStreamTest()
    {
        //Arrange
        var fileStoreEntry = await _store.GetFileInfo(myFile);
        //Act
        var stream = await _store.GetFileStream(fileStoreEntry);
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
        var result =
            await _store.CreateFileFromStream("file456.txt", new MemoryStream(Encoding.UTF8.GetBytes("test")), true);
        //Act
        var stream = await _store.GetFileStream("file456.txt");
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
        var result =
            await _store.CreateFileFromStream("file456.txt", new MemoryStream(Encoding.UTF8.GetBytes("test")), true);
        //Assert
        Assert.AreEqual("file456.txt", result);
    }

    [TestMethod]
    public async Task ReadAllTextTest()
    {
        await _store.WriteAllText("file123.txt", "test");
        //Act
        var result = await _store.ReadAllText("file123.txt");
        //Assert
        Assert.AreEqual("test", result);
    }

    [TestMethod]
    public async Task WriteAllTextTest()
    {
        //Act
        await _store.WriteAllText("file111.txt", "test");

        //Assert
        var result = await _store.ReadAllText("file111.txt");
        Assert.AreEqual("test", result);
    }
}