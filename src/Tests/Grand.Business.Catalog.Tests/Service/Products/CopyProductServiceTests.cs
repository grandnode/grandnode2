using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Catalog.Services.Products;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Seo;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Grand.Domain.Seo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Tests.Service.Products
{
    [TestClass()]
    public class CopyProductServiceTests
    {
        private Mock<IProductService> _productServiceMock;
        private Mock<ILanguageService> _langServiceMock;
        private Mock<IPictureService> _pictureServiceMock;
        private Mock<IDownloadService> _downloadServiceMock;
        private Mock<ISlugService> _slugServiceMock;
        private SeoSettings _settings;
        private CopyProductService _copyProductService;

        [TestInitialize()]
        public void Init()
        {
            _productServiceMock = new Mock<IProductService>();
            _langServiceMock = new Mock<ILanguageService>();
            _pictureServiceMock = new Mock<IPictureService>();
            _downloadServiceMock = new Mock<IDownloadService>();
            _slugServiceMock = new Mock<ISlugService>();
            _settings = new SeoSettings();
            _copyProductService = new CopyProductService(_productServiceMock.Object, _langServiceMock.Object, _pictureServiceMock.Object,
                _downloadServiceMock.Object, _slugServiceMock.Object, _settings);
        }


        [TestMethod()]
        public async Task CopyProduct_InserAndReturnNewProduct()
        {
            _langServiceMock.Setup(c => c.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>())).Returns(Task.FromResult<IList<Language>>(new List<Language>()));
            var product = new Product()
            {
                Name = "name",
                Price = 49,
                CreatedOnUtc = DateTime.UtcNow,
                ShortDescription="Desc"
            };
            var copy = await _copyProductService.CopyProduct(product, "copy-product");
            Assert.AreEqual(copy.Price, product.Price);
            Assert.AreEqual(copy.ShortDescription, product.ShortDescription);
 

            Assert.AreNotEqual(copy.SeName, product.SeName);
            Assert.AreNotEqual(copy.Id, product.Id);
            Assert.AreNotEqual(copy.CreatedOnUtc, product.CreatedOnUtc);
            _productServiceMock.Verify(c => c.InsertProduct(It.IsAny<Product>()), Times.Once);
        }
    }
}
