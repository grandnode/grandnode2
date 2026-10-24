using Grand.Business.Storage.Services;
using Grand.Domain.Media;
using Grand.SharedKernel.Extensions;
using Grand.Web.AdminShared.Models.FileManager;
using Grand.Web.AdminShared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Text;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class FileManagerServiceTests
{
    private string _webRoot;
    private string _uploaded;
    private FileManagerService _service;

    [TestInitialize]
    public void Init()
    {
        _webRoot = Path.Combine(Path.GetTempPath(), "filemanager-" + Guid.NewGuid().ToString("N"));
        _uploaded = Path.Combine(_webRoot, "assets", "images", "uploaded");
        Directory.CreateDirectory(_uploaded);
        //what a path that escapes the root would reach: the site's own scripts
        Directory.CreateDirectory(Path.Combine(_webRoot, "assets", "custom"));
        File.WriteAllText(Path.Combine(_webRoot, "assets", "custom", "site.js"), "script");

        _service = Create(null);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_webRoot)) Directory.Delete(_webRoot, true);
    }

    private FileManagerService Create(string directory)
    {
        var configuration = new Mock<IConfiguration>();
        configuration.Setup(c => c[CommonPath.DirectoryParam]).Returns(directory);
        var store = new DefaultMediaFileStore(new FileSystemStore(_webRoot));
        return new FileManagerService(store, new MediaSettings { AllowedFileTypes = ".jpg, .png, .mp4" }, configuration.Object);
    }

    private static FileManagerUpload Upload(string name, string content = "data")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return new FileManagerUpload(name, bytes.Length, () => new MemoryStream(bytes));
    }

    [TestMethod]
    [DataRow("..")]
    [DataRow("../custom")]
    [DataRow("a/../../custom")]
    [DataRow("..\\..\\custom")]
    [DataRow("./a")]
    [DataRow("a//b")]
    [DataRow("C:")]
    [DataRow(".git")]
    [DataRow("con")]
    [DataRow("a/nul.txt")]
    [DataRow("a/b?")]
    [DataRow("trailing.")]
    public void TryGetSegments_PathThatCouldLeaveTheRoot_Fails(string path)
    {
        Assert.IsFalse(FileManagerService.TryGetSegments(path, out _));
    }

    [TestMethod]
    [DataRow(null, "")]
    [DataRow("", "")]
    [DataRow("/", "")]
    [DataRow("banners", "banners")]
    [DataRow("/banners/2026/", "banners|2026")]
    [DataRow("banners\\2026", "banners|2026")]
    [DataRow("summer sale/top.jpg", "summer sale|top.jpg")]
    public void TryGetSegments_PathUnderTheRoot_Splits(string path, string expected)
    {
        Assert.IsTrue(FileManagerService.TryGetSegments(path, out var segments));
        Assert.AreEqual(expected, string.Join('|', segments));
    }

    [TestMethod]
    public void List_Root_FoldersFirstAndOnlyAllowedTypes()
    {
        Directory.CreateDirectory(Path.Combine(_uploaded, "zeta"));
        File.WriteAllText(Path.Combine(_uploaded, "a.png"), "x");
        File.WriteAllText(Path.Combine(_uploaded, "placeholder.txt"), "x");
        File.WriteAllText(Path.Combine(_uploaded, ".hidden.png"), "x");

        var listing = _service.List("");

        Assert.AreEqual(FileManagerError.None, listing.Error);
        CollectionAssert.AreEqual(new[] { "zeta", "a.png" }, listing.Entries.Select(e => e.Name).ToArray());
        Assert.IsTrue(listing.Entries[0].IsFolder);
        Assert.AreEqual("image", listing.Entries[1].Kind);
        Assert.AreEqual("/assets/images/uploaded/a.png", listing.Entries[1].Url);
    }

    [TestMethod]
    public void List_MissingRoot_IsCreated()
    {
        Directory.Delete(_uploaded, true);

        var listing = _service.List(null);

        Assert.AreEqual(FileManagerError.None, listing.Error);
        Assert.IsTrue(Directory.Exists(_uploaded));
    }

    [TestMethod]
    public void List_NestedFolder_RelativePathsCrumbsAndEscapedUrls()
    {
        Directory.CreateDirectory(Path.Combine(_uploaded, "summer sale"));
        File.WriteAllText(Path.Combine(_uploaded, "summer sale", "top #1.jpg"), "x");

        var listing = Create("tenant").List("summer sale");

        Assert.AreEqual("summer sale", listing.Path);
        CollectionAssert.AreEqual(new[] { "", "summer sale" }, listing.Crumbs.Select(c => c.Path).ToArray());
        Assert.AreEqual("summer sale/top #1.jpg", listing.Entries.Single().Path);
        Assert.AreEqual("/tenant/assets/images/uploaded/summer%20sale/top%20%231.jpg", listing.Entries.Single().Url);
    }

    [TestMethod]
    public void List_Traversal_IsRefused()
    {
        Assert.AreEqual(FileManagerError.InvalidPath, _service.List("../custom").Error);
    }

    [TestMethod]
    public void List_MissingFolder_NotFound()
    {
        Assert.AreEqual(FileManagerError.NotFound, _service.List("nope").Error);
    }

    [TestMethod]
    public async Task Upload_AllowedFile_IsWrittenInTheFolder()
    {
        Directory.CreateDirectory(Path.Combine(_uploaded, "banners"));

        var result = await _service.Upload("banners", [Upload("top.jpg")]);

        Assert.AreEqual(FileManagerError.None, result.Error);
        Assert.AreEqual("banners/top.jpg", result.Uploaded.Single().Path);
        Assert.IsTrue(File.Exists(Path.Combine(_uploaded, "banners", "top.jpg")));
    }

    [TestMethod]
    public async Task Upload_TakenName_GetsANumberAndNothingIsReplaced()
    {
        File.WriteAllText(Path.Combine(_uploaded, "top.jpg"), "original");

        var result = await _service.Upload("", [Upload("top.jpg", "new"), Upload("top.jpg", "newer")]);

        CollectionAssert.AreEqual(new[] { "top (1).jpg", "top (2).jpg" }, result.Uploaded.Select(u => u.Name).ToArray());
        Assert.AreEqual("original", File.ReadAllText(Path.Combine(_uploaded, "top.jpg")));
    }

    [TestMethod]
    public async Task Upload_ClientPathInTheName_KeepsOnlyTheName()
    {
        var result = await _service.Upload("", [Upload("..\\..\\custom\\x.png")]);

        Assert.AreEqual("x.png", result.Uploaded.Single().Name);
        Assert.IsTrue(File.Exists(Path.Combine(_uploaded, "x.png")));
    }

    [TestMethod]
    public async Task Upload_TypeNotAllowedTooLargeOrBadName_IsRejected()
    {
        var big = new FileManagerUpload("big.png", FileManagerService.MaxUploadBytes + 1, () => new MemoryStream());

        var result = await _service.Upload("", [Upload("page.cshtml"), Upload("run.js"), big, Upload(".png")]);

        Assert.AreEqual(0, result.Uploaded.Count);
        CollectionAssert.AreEqual(
            new[] { FileManagerError.NotAllowedType, FileManagerError.NotAllowedType, FileManagerError.TooLarge, FileManagerError.InvalidName },
            result.Rejected.Select(r => r.Error).ToArray());
        Assert.AreEqual(0, Directory.GetFiles(_uploaded).Length);
    }

    [TestMethod]
    public async Task Upload_Traversal_IsRefused()
    {
        var result = await _service.Upload("../custom", [Upload("x.png")]);

        Assert.AreEqual(FileManagerError.InvalidPath, result.Error);
        Assert.IsFalse(File.Exists(Path.Combine(_webRoot, "assets", "custom", "x.png")));
    }

    [TestMethod]
    public async Task CreateFolder_ValidName_IsCreated()
    {
        var result = await _service.CreateFolder("", " banners ");

        Assert.AreEqual(FileManagerError.None, result.Error);
        Assert.AreEqual("banners", result.Entry.Path);
        Assert.IsTrue(Directory.Exists(Path.Combine(_uploaded, "banners")));
    }

    [TestMethod]
    [DataRow("..")]
    [DataRow("a/b")]
    [DataRow("")]
    [DataRow(".git")]
    public async Task CreateFolder_InvalidName_IsRefused(string name)
    {
        Assert.AreEqual(FileManagerError.InvalidName, (await _service.CreateFolder("", name)).Error);
    }

    [TestMethod]
    public async Task CreateFolder_TakenName_AlreadyExists()
    {
        File.WriteAllText(Path.Combine(_uploaded, "banners"), "a file");

        Assert.AreEqual(FileManagerError.AlreadyExists, (await _service.CreateFolder("", "banners")).Error);
    }

    [TestMethod]
    public async Task Rename_File_KeepsAnAllowedType()
    {
        File.WriteAllText(Path.Combine(_uploaded, "a.png"), "x");

        Assert.AreEqual(FileManagerError.NotAllowedType, (await _service.Rename("a.png", "a.js")).Error);
        Assert.AreEqual(FileManagerError.NotAllowedType, (await _service.Rename("a.png", "a.cshtml")).Error);
        var ok = await _service.Rename("a.png", "b.png");

        Assert.AreEqual(FileManagerError.None, ok.Error);
        Assert.AreEqual("b.png", ok.Entry.Path);
        Assert.IsTrue(File.Exists(Path.Combine(_uploaded, "b.png")));
    }

    [TestMethod]
    public async Task Rename_Folder_Moves()
    {
        Directory.CreateDirectory(Path.Combine(_uploaded, "old", "inner"));

        var result = await _service.Rename("old", "new");

        Assert.AreEqual(FileManagerError.None, result.Error);
        Assert.IsTrue(Directory.Exists(Path.Combine(_uploaded, "new", "inner")));
        Assert.IsFalse(Directory.Exists(Path.Combine(_uploaded, "old")));
    }

    [TestMethod]
    public async Task Rename_OntoAnotherEntry_AlreadyExists()
    {
        File.WriteAllText(Path.Combine(_uploaded, "a.png"), "a");
        File.WriteAllText(Path.Combine(_uploaded, "b.png"), "b");

        Assert.AreEqual(FileManagerError.AlreadyExists, (await _service.Rename("a.png", "b.png")).Error);
        Assert.AreEqual("b", File.ReadAllText(Path.Combine(_uploaded, "b.png")));
    }

    [TestMethod]
    public async Task Rename_CaseOnly_RenamesTheFileAndTheFolder()
    {
        File.WriteAllText(Path.Combine(_uploaded, "Logo.png"), "x");
        Directory.CreateDirectory(Path.Combine(_uploaded, "Banners"));

        var file = await _service.Rename("Logo.png", "logo.png");
        var folder = await _service.Rename("Banners", "banners");

        Assert.AreEqual(FileManagerError.None, file.Error);
        Assert.AreEqual(FileManagerError.None, folder.Error);
        CollectionAssert.AreEquivalent(new[] { "logo.png" }, Directory.GetFiles(_uploaded).Select(Path.GetFileName).ToArray());
        CollectionAssert.AreEquivalent(new[] { "banners" }, Directory.GetDirectories(_uploaded).Select(Path.GetFileName).ToArray());
    }

    [TestMethod]
    public async Task Rename_NameWithAPath_IsRefused()
    {
        File.WriteAllText(Path.Combine(_uploaded, "a.png"), "a");

        Assert.AreEqual(FileManagerError.InvalidName, (await _service.Rename("a.png", "../../custom/a.png")).Error);
    }

    [TestMethod]
    public async Task RenameAndDelete_Root_AreLocked()
    {
        Assert.AreEqual(FileManagerError.RootLocked, (await _service.Rename("", "x")).Error);
        Assert.AreEqual(FileManagerError.RootLocked, (await _service.Delete("/")).Error);
        Assert.IsTrue(Directory.Exists(_uploaded));
    }

    [TestMethod]
    public async Task Delete_FolderWithContent_IsRemoved()
    {
        Directory.CreateDirectory(Path.Combine(_uploaded, "old"));
        File.WriteAllText(Path.Combine(_uploaded, "old", "a.png"), "x");

        Assert.AreEqual(FileManagerError.None, (await _service.Delete("old")).Error);
        Assert.IsFalse(Directory.Exists(Path.Combine(_uploaded, "old")));
    }

    [TestMethod]
    public async Task Delete_Traversal_LeavesTheSiteAlone()
    {
        Assert.AreEqual(FileManagerError.InvalidPath, (await _service.Delete("../../custom/site.js")).Error);
        Assert.AreEqual(FileManagerError.InvalidPath, (await _service.Delete("..")).Error);
        Assert.IsTrue(File.Exists(Path.Combine(_webRoot, "assets", "custom", "site.js")));
    }

    [TestMethod]
    public async Task Delete_Missing_NotFound()
    {
        Assert.AreEqual(FileManagerError.NotFound, (await _service.Delete("nope.png")).Error);
    }
}
