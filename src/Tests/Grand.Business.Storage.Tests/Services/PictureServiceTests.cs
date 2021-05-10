using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Storage.Interfaces;
using Grand.Business.Storage.Services;
using Grand.Domain.Data;
using Grand.Domain.Media;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Storage.Tests.Services
{
    [TestClass]
    public class PictureServiceTests
    {
        private Mock<IRepository<Picture>> _repoMock;
        private Mock<ILogger> _logerMock;
        private Mock<IMediator> _mediatorMock;
        private Mock<IWebHostEnvironment> _webHostMock;
        private Mock<IWorkContext> _workContextMock;
        private Mock<ICacheBase> _cacheMock;
        private Mock<IMediaFileStore> _mediaFileStoreMock;

        private MediaSettings _settings;
        private PictureService _service;

        [TestInitialize]
        public void Init()
        {
            _repoMock = new Mock<IRepository<Picture>>();
            _logerMock = new Mock<ILogger>();
            _mediatorMock = new Mock<IMediator>();
            _webHostMock = new Mock<IWebHostEnvironment>();
            _workContextMock = new Mock<IWorkContext>();
            _cacheMock = new Mock<ICacheBase>();
            _mediaFileStoreMock = new Mock<IMediaFileStore>();
            _settings = new MediaSettings();
            _service = new PictureService(_repoMock.Object,_logerMock.Object,_mediatorMock.Object,_webHostMock.Object,_workContextMock.Object
                ,_cacheMock.Object, _mediaFileStoreMock.Object, _settings);
        }

        [TestMethod]
        public async Task LoadPictureBinary_FromDb_InvokeRepository()
        {
            _repoMock.Setup(c => c.GetByIdAsync(It.IsAny<string>())).Returns(Task.FromResult(new Picture() { PictureBinary= new byte[] { } }));
            await _service.LoadPictureBinary(new Picture(), true);
            _repoMock.Verify(c => c.GetByIdAsync(It.IsAny<string>()), Times.Once);
        }


        [TestMethod]
        public async Task LoadPictureBinary_FromFile_ReturnEmptyBytes()
        {
            _webHostMock.Setup(c => c.WebRootPath).Returns("~root/");
            var result=await _service.LoadPictureBinary(new Picture() { Id="id",MimeType= "image/jpeg" }, false);
            //we can't mock static class like File.Exist so, should return empty array
            Assert.IsTrue(result.Length == 0);
        }

        [TestMethod]
        public async Task GetPictureUrl_InovkeCacheQuery()
        {
            await _service.GetPictureUrl("picture id");
            _cacheMock.Verify(c => c.GetAsync<string>(It.IsAny<string>(), It.IsAny<Func<Task<string>>>()), Times.Once); 
        }

        [TestMethod]
        public async Task InsertPicture_InvokeExpectedMethods()
        {
            _settings.StoreInDb = true;
            await _service.InsertPicture(new byte[] { }, "image/jpeg", "image", validateBinary: false);
            _repoMock.Verify(c => c.InsertAsync(It.IsAny<Picture>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<Picture>>(), default), Times.Once);
        }

        [TestMethod]
        public async Task UpdatePicture_InvokeExpectedMethods()
        {
            _settings.StoreInDb = true;
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
    }
}
