using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class ProductAddPopupViewModelServiceTests
{
    private Mock<IProductService> _productServiceMock;
    private Mock<IVendorService> _vendorServiceMock;
    private Mock<IAdminDataScope<Product>> _scopeMock;
    private ProductAddPopupViewModelService _productAddPopupViewModelService;

    [TestInitialize]
    public void Setup()
    {
        _productServiceMock = new Mock<IProductService>();
        _vendorServiceMock = new Mock<IVendorService>();

        var storeServiceMock = new Mock<IStoreService>();
        storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Store>());

        var translationServiceMock = new Mock<IRequestTranslationService>();
        translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        var enumTranslationServiceMock = new Mock<IEnumTranslationService>();
        enumTranslationServiceMock
            .Setup(e => e.ToSelectList(It.IsAny<ProductType>(), It.IsAny<bool>(), It.IsAny<int[]>()))
            .Returns(new SelectList(Enumerable.Empty<SelectListItem>()));

        // Default: Admin's Global scope - no default store/vendor, store dropdown shows.
        _scopeMock = new Mock<IAdminDataScope<Product>>();
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _scopeMock.Setup(s => s.ShowStoreSelector).Returns(true);
        _scopeMock.Setup(s => s.DefaultVendorId).Returns((string)null);

        _productAddPopupViewModelService = new ProductAddPopupViewModelService(
            _productServiceMock.Object,
            new Mock<IDateTimeService>().Object,
            _vendorServiceMock.Object,
            storeServiceMock.Object,
            translationServiceMock.Object,
            enumTranslationServiceMock.Object,
            _scopeMock.Object);
    }

    [TestMethod]
    public async Task PrepareProductModel_AddProductModel_VendorScope_OverridesModelVendorId()
    {
        _scopeMock.Setup(s => s.DefaultVendorId).Returns("vendor1");
        _productServiceMock.Setup(p => p.SearchProducts(
                It.IsAny<CatalogAccess>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IList<string>>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ProductType?>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<bool?>(),
                It.IsAny<double?>(), It.IsAny<double?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<IList<string>>(), It.IsAny<IList<string>>(),
                It.IsAny<ProductSortingEnum>(), It.IsAny<bool?>()))
            .ReturnsAsync((new PagedList<Product>(), new List<string>()));

        var model = new ProductModel.AddRelatedProductModel { SearchVendorId = "vendor2" };
        await _productAddPopupViewModelService.PrepareProductModel(model, 1, 10);

        _productServiceMock.Verify(p => p.SearchProducts(
            It.IsAny<CatalogAccess>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IList<string>>(),
            It.IsAny<string>(), It.IsAny<string>(), "vendor1", It.IsAny<string>(),
            It.IsAny<ProductType?>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<bool?>(),
            It.IsAny<double?>(), It.IsAny<double?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<IList<string>>(), It.IsAny<IList<string>>(),
            It.IsAny<ProductSortingEnum>(), It.IsAny<bool?>()), Times.Once);
    }

    [TestMethod]
    public async Task PrepareRelatedProductModel_VendorScope_HidesVendorDropdown()
    {
        _scopeMock.Setup(s => s.DefaultVendorId).Returns("vendor1");

        var model = await _productAddPopupViewModelService.PrepareRelatedProductModel();

        Assert.IsFalse(model.AvailableVendors.Any(),
            "Vendor's original PrepareAddProductModel<T> never populated a vendor picker - the search is always forced to the current vendor.");
        _vendorServiceMock.Verify(v => v.GetAllVendors(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()),
            Times.Never);
    }

    [TestMethod]
    public async Task PrepareRelatedProductModel_GlobalScope_IncludesVendorDropdown()
    {
        _vendorServiceMock.Setup(v => v.GetAllVendors(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), true))
            .ReturnsAsync(new PagedList<Vendor> { new() { Id = "vendor1", Name = "Vendor 1" } });

        var model = await _productAddPopupViewModelService.PrepareRelatedProductModel();

        Assert.IsTrue(model.AvailableVendors.Any(x => x.Value == "vendor1"),
            "Admin/Store should keep offering a vendor picker.");
    }
}
