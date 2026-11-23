using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Common;
using Grand.Domain.Media;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BasePictureControllerTests
{
    // Concrete subclass exists only to instantiate the abstract base for direct-call unit tests -
    // same pattern as BaseTaxCategoryControllerTests/BaseEmailAccountControllerTests.
    private class TestPictureController(
        IPictureService pictureService,
        IRequestPermissionService permissionService,
        IMediaFileStore mediaFileStore,
        MediaSettings mediaSettings)
        : BasePictureController(pictureService, permissionService, mediaFileStore, mediaSettings);

    private Mock<IPictureService> _pictureServiceMock = null!;
    private Mock<IRequestPermissionService> _permissionServiceMock = null!;
    private Mock<IMediaFileStore> _mediaFileStoreMock = null!;
    private MediaSettings _mediaSettings = null!;

    [TestInitialize]
    public void Setup()
    {
        _pictureServiceMock = new Mock<IPictureService>();
        _permissionServiceMock = new Mock<IRequestPermissionService>();
        _mediaFileStoreMock = new Mock<IMediaFileStore>();
        _mediaSettings = new MediaSettings { AllowedFileTypes = ".jpg,.png" };
    }

    private TestPictureController CreateController()
    {
        return new TestPictureController(
            _pictureServiceMock.Object,
            _permissionServiceMock.Object,
            _mediaFileStoreMock.Object,
            _mediaSettings);
    }

    private static Mock<IFormFile> CreateFormFile(string fileName, string contentType = "image/jpeg")
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.ContentType).Returns(contentType);
        file.Setup(f => f.Length).Returns(1);
        return file;
    }

    // The controller's anonymous JSON payloads are internal to Grand.Web.AdminShared - `dynamic`
    // binding can't reach their properties across the assembly boundary, so read them via
    // reflection, same pattern as BaseDiscountControllerTests/BaseMerchandiseReturnControllerTests.
    private static T GetValue<T>(IActionResult result, string propertyName)
    {
        var value = ((JsonResult)result).Value!;
        return (T)value.GetType().GetProperty(propertyName)!.GetValue(value)!;
    }

    [TestMethod]
    public async Task AsyncUpload_NullFile_ReturnsFailureJson()
    {
        var controller = CreateController();

        var result = await controller.AsyncUpload(null);

        Assert.IsFalse(GetValue<bool>(result, "success"));
        Assert.AreEqual("No file uploaded", GetValue<string>(result, "message"));
        _pictureServiceMock.Verify(p => p.InsertPicture(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Reference>(), It.IsAny<string>(),
            It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task AsyncUpload_ReferenceWithoutObjectId_ReturnsFailureJson()
    {
        var controller = CreateController();
        var file = CreateFormFile("picture.jpg");

        var result = await controller.AsyncUpload(file.Object, Reference.Product, "");

        Assert.IsFalse(GetValue<bool>(result, "success"));
        Assert.AreEqual("Please save form before upload new picture", GetValue<string>(result, "message"));
    }

    [TestMethod]
    public async Task AsyncUpload_DisallowedFileType_ReturnsFailureJson()
    {
        var controller = CreateController();
        var file = CreateFormFile("picture.exe");

        var result = await controller.AsyncUpload(file.Object);

        Assert.IsFalse(GetValue<bool>(result, "success"));
        _pictureServiceMock.Verify(p => p.InsertPicture(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Reference>(), It.IsAny<string>(),
            It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task AsyncUpload_AllowedFileType_InsertsPictureAndReturnsSuccessJson()
    {
        var controller = CreateController();
        var file = CreateFormFile("picture.jpg");
        using var fileStream = new MemoryStream([1, 2, 3]);
        file.Setup(f => f.OpenReadStream()).Returns(fileStream);
        var insertedPicture = new Picture { Id = "pic-1" };
        _pictureServiceMock
            .Setup(p => p.InsertPicture(It.IsAny<byte[]>(), "image/jpeg", null,
                null, null, true, Reference.Product, "obj-1", false))
            .ReturnsAsync(insertedPicture);
        _pictureServiceMock
            .Setup(p => p.GetPictureUrl(insertedPicture, 100, true, null))
            .ReturnsAsync("/media/pic-1.jpg");

        var result = await controller.AsyncUpload(file.Object, Reference.Product, "obj-1");

        Assert.IsTrue(GetValue<bool>(result, "success"));
        Assert.AreEqual("pic-1", GetValue<string>(result, "pictureId"));
        Assert.AreEqual("/media/pic-1.jpg", GetValue<string>(result, "imageUrl"));
    }

    [TestMethod]
    public async Task AsyncLogoUpload_PermissionDenied_ReturnsAccessDenied()
    {
        _permissionServiceMock.Setup(p => p.Authorize(StandardPermission.ManageSettings)).ReturnsAsync(false);
        var controller = CreateController();
        var file = CreateFormFile("logo.jpg");

        var result = await controller.AsyncLogoUpload(file.Object);

        var content = (ContentResult)result;
        Assert.AreEqual("Access denied", content.Content);
        _mediaFileStoreMock.Verify(m => m.GetFileInfo(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task AsyncLogoUpload_NullFile_ReturnsFailureJson()
    {
        _permissionServiceMock.Setup(p => p.Authorize(StandardPermission.ManageSettings)).ReturnsAsync(true);
        var controller = CreateController();

        var result = await controller.AsyncLogoUpload(null);

        Assert.IsFalse(GetValue<bool>(result, "success"));
        Assert.AreEqual("No file uploaded", GetValue<string>(result, "message"));
    }

    [TestMethod]
    public async Task AsyncLogoUpload_DisallowedFileType_ReturnsFailureJson()
    {
        _permissionServiceMock.Setup(p => p.Authorize(StandardPermission.ManageSettings)).ReturnsAsync(true);
        var controller = CreateController();
        var file = CreateFormFile("logo.exe");

        var result = await controller.AsyncLogoUpload(file.Object);

        Assert.IsFalse(GetValue<bool>(result, "success"));
        Assert.AreEqual("File no allowed", GetValue<string>(result, "message"));
        _mediaFileStoreMock.Verify(m => m.GetFileInfo(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task AsyncLogoUpload_NameTaken_KeepsTheExistingFile_AndSavesUnderAFreeName()
    {
        _permissionServiceMock.Setup(p => p.Authorize(StandardPermission.ManageSettings)).ReturnsAsync(true);
        var folder = Directory.CreateDirectory(Path.Join(Path.GetTempPath(), "grand-logo-" + Guid.NewGuid())).FullName;
        try
        {
            await File.WriteAllTextAsync(Path.Join(folder, "logo.png"), "another store's logo");
            var root = new Mock<IFileStoreEntry>();
            root.Setup(r => r.PhysicalPath).Returns(folder);
            _mediaFileStoreMock.Setup(m => m.GetDirectoryInfo("")).Returns(root.Object);
            _mediaFileStoreMock.Setup(m => m.GetFileInfo(It.IsAny<string>()))
                .ReturnsAsync((string name) => File.Exists(Path.Join(folder, name)) ? Mock.Of<IFileStoreEntry>() : null);
            var file = CreateFormFile("logo.png", "image/png");
            file.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns((Stream target, CancellationToken _) => target.WriteAsync("new"u8.ToArray()).AsTask());

            var result = await CreateController().AsyncLogoUpload(file.Object);

            Assert.IsTrue(GetValue<bool>(result, "success"));
            Assert.AreEqual("logo-1.png", GetValue<string>(result, "imageUrl"));
            Assert.AreEqual("another store's logo", await File.ReadAllTextAsync(Path.Join(folder, "logo.png")));
            Assert.AreEqual("new", await File.ReadAllTextAsync(Path.Join(folder, "logo-1.png")));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}

