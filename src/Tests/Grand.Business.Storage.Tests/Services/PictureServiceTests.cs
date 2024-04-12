using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Storage.Services;
using Grand.Data;
using Grand.Domain.Media;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Storage.Tests.Services;

[TestClass]
public class PictureServiceTests
{
    private Mock<ICacheBase> _cacheMock;
    private Mock<ILogger<PictureService>> _logerMock;
    private Mock<IMediaFileStore> _mediaFileStoreMock;
    private Mock<IMediator> _mediatorMock;
    private Mock<IRepository<Picture>> _repoMock;
    private PictureService _service;

    private MediaSettings _settings;
    private StorageSettings _storagesettings;
    private Mock<IWebHostEnvironment> _webHostMock;
    private Mock<IWorkContext> _workContextMock;

    [TestInitialize]
    public void Init()
    {
        _webHostMock = new Mock<IWebHostEnvironment>();
        _repoMock = new Mock<IRepository<Picture>>();
        _logerMock = new Mock<ILogger<PictureService>>();
        _mediatorMock = new Mock<IMediator>();
        _workContextMock = new Mock<IWorkContext>();
        _cacheMock = new Mock<ICacheBase>();
        _mediaFileStoreMock = new Mock<IMediaFileStore>();
        _settings = new MediaSettings();
        _storagesettings = new StorageSettings();
        _service = new PictureService(_repoMock.Object, _logerMock.Object, _mediatorMock.Object, _cacheMock.Object,
            _mediaFileStoreMock.Object, _settings, _storagesettings);
    }

    [TestMethod]
    public async Task GetPictureById_InvokeExpectedMethods()
    {
        await _service.GetPictureById("1");
        _cacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<Picture>>>()), Times.Once);
    }

    [TestMethod]
    public async Task LoadPictureBinary_FromDb()
    {
        _storagesettings.PictureStoreInDb = true;
        _repoMock.Setup(c => c.GetByIdAsync(It.IsAny<string>())).Returns(Task.FromResult(new Picture {
            PictureBinary =
                []
        }));
        await _service.LoadPictureBinary(new Picture());
        _repoMock.Verify(c => c.GetByIdAsync(It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task LoadPictureBinary_FromDb_InvokeRepository()
    {
        _repoMock.Setup(c => c.GetByIdAsync(It.IsAny<string>())).Returns(Task.FromResult(new Picture {
            PictureBinary =
                []
        }));
        await _service.LoadPictureBinary(new Picture(), true);
        _repoMock.Verify(c => c.GetByIdAsync(It.IsAny<string>()), Times.Once);
    }


    [TestMethod]
    public async Task LoadPictureBinary_FromFile_ReturnEmptyBytes()
    {
        _webHostMock.Setup(c => c.WebRootPath).Returns("~root/");
        var result = await _service.LoadPictureBinary(new Picture { Id = "id", MimeType = "image/jpeg" }, false);
        //we can't mock static class like File.Exist so, should return empty array
        Assert.IsTrue(result.Length == 0);
    }

    [TestMethod]
    public async Task GetPictureUrl_InovkeCacheQuery()
    {
        await _service.GetPictureUrl("picture id");
        _cacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<string>>>()), Times.Once);
    }

    [TestMethod]
    public async Task InsertPicture_InvokeExpectedMethods()
    {
        _storagesettings.PictureStoreInDb = true;
        await _service.InsertPicture([], "image/jpeg", "image", validateBinary: false);
        _repoMock.Verify(c => c.InsertAsync(It.IsAny<Picture>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<Picture>>(), default), Times.Once);
    }

    [TestMethod]
    public async Task UpdatePicture_InvokeExpectedMethods_PublishEvent()
    {
        _storagesettings.PictureStoreInDb = true;
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<Picture>>>()))
            .Returns(Task.FromResult(new Picture()));
        _repoMock.Setup(c => c.GetByIdAsync(It.IsAny<string>())).Returns(Task.FromResult(new Picture {
            PictureBinary =
                []
        }));
        await _service.UpdatePicture("1", [], "image/jpeg", "image", validateBinary: false);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<Picture>>(), default), Times.Once);
    }

    [TestMethod]
    public async Task UpdatePicture_PictureModel_InvokeExpectedMethods()
    {
        _storagesettings.PictureStoreInDb = true;
        await _service.UpdatePicture(new Picture());
        _repoMock.Verify(c => c.UpdateAsync(It.IsAny<Picture>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<Picture>>(), default), Times.Once);
    }

    [TestMethod]
    public void GetPictureSeName_ReturnExpectedValue()
    {
        Assert.AreEqual("productname", _service.GetPictureSeName("productName"));
        Assert.AreEqual("product-name", _service.GetPictureSeName("product-Name"));
        Assert.AreEqual("product-name", _service.GetPictureSeName("Product-Name"));
        Assert.AreEqual("productname-2", _service.GetPictureSeName("ProductName-2"));
    }

    [TestMethod]
    public async Task DeletePicture_ValidArguments_InvokeRepositoryAndPublishEvent()
    {
        await _service.DeletePicture(new Picture());
        _repoMock.Verify(c => c.DeleteAsync(It.IsAny<Picture>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<Picture>>(), default), Times.Once);
    }
}