using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Marketing.Customers;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Vendors;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Moq;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class CustomerProductViewModelServiceTests
{
    private Mock<ICustomerProductService> _customerProductServiceMock;
    private Mock<IProductService> _productServiceMock;
    private Mock<IStoreService> _storeServiceMock;
    private Mock<IVendorService> _vendorServiceMock;
    private Mock<IEnumTranslationService> _enumTranslationServiceMock;
    private CustomerProductViewModelService _customerProductViewModelService;

    [TestInitialize]
    public void Setup()
    {
        _customerProductServiceMock = new Mock<ICustomerProductService>();
        _productServiceMock = new Mock<IProductService>();
        _storeServiceMock = new Mock<IStoreService>();
        _vendorServiceMock = new Mock<IVendorService>();
        _enumTranslationServiceMock = new Mock<IEnumTranslationService>();
        var translationServiceMock = new Mock<IRequestTranslationService>();
        translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns<string>(k => k);

        _customerProductViewModelService = new CustomerProductViewModelService(
            _customerProductServiceMock.Object,
            _productServiceMock.Object,
            _storeServiceMock.Object,
            _vendorServiceMock.Object,
            translationServiceMock.Object,
            _enumTranslationServiceMock.Object);
    }

    [TestMethod]
    public async Task PrepareCustomerModelAddProductModel_BuildsAvailableLists()
    {
        _storeServiceMock.Setup(s => s.GetAllStores())
            .ReturnsAsync(new List<Grand.Domain.Stores.Store> { new() { Id = "store-1", Shortcut = "Store 1" } });
        _vendorServiceMock
            .Setup(v => v.GetAllVendors(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync(new PagedList<Vendor> { new() { Id = "vendor-1", Name = "Vendor 1" } });
        _enumTranslationServiceMock
            .Setup(e => e.ToSelectList(It.IsAny<ProductType>(), It.IsAny<bool>(), It.IsAny<int[]>()))
            .Returns(new SelectList(Enumerable.Empty<SelectListItem>()));

        var model = await _customerProductViewModelService.PrepareCustomerModelAddProductModel();

        Assert.IsTrue(model.AvailableStores.Any(x => x.Value == "store-1"));
        Assert.IsTrue(model.AvailableVendors.Any(x => x.Value == "vendor-1"));
        Assert.IsTrue(model.AvailableProductTypes.Count > 0);
    }

    [TestMethod]
    public async Task UpdateProductPrice_UpdatesWhenFound()
    {
        var price = new CustomerProductPrice { Id = "pp1", Price = 1 };
        _customerProductServiceMock.Setup(c => c.GetCustomerProductPriceById("pp1")).ReturnsAsync(price);
        _customerProductServiceMock.Setup(c => c.UpdateCustomerProductPrice(It.IsAny<CustomerProductPrice>()))
            .Returns(Task.CompletedTask);

        await _customerProductViewModelService.UpdateProductPrice(
            new CustomerModel.ProductPriceModel { Id = "pp1", Price = 99 });

        _customerProductServiceMock.Verify(
            c => c.UpdateCustomerProductPrice(It.Is<CustomerProductPrice>(x => x.Price == 99)), Times.Once);
    }

    [TestMethod]
    public async Task DeleteProductPrice_DeletesWhenFound()
    {
        var price = new CustomerProductPrice { Id = "pp1" };
        _customerProductServiceMock.Setup(c => c.GetCustomerProductPriceById("pp1")).ReturnsAsync(price);
        _customerProductServiceMock.Setup(c => c.DeleteCustomerProductPrice(It.IsAny<CustomerProductPrice>()))
            .Returns(Task.CompletedTask);

        await _customerProductViewModelService.DeleteProductPrice("pp1");

        _customerProductServiceMock.Verify(c => c.DeleteCustomerProductPrice(price), Times.Once);
    }

    [TestMethod]
    public async Task DeleteProductPrice_NotFound_Throws()
    {
        _customerProductServiceMock.Setup(c => c.GetCustomerProductPriceById(It.IsAny<string>()))
            .ReturnsAsync((CustomerProductPrice)null);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _customerProductViewModelService.DeleteProductPrice("missing"));
    }

    [TestMethod]
    public async Task UpdatePersonalizedProduct_UpdatesWhenFound()
    {
        var customerProduct = new CustomerProduct { Id = "cp1", DisplayOrder = 1 };
        _customerProductServiceMock.Setup(c => c.GetCustomerProduct("cp1")).ReturnsAsync(customerProduct);
        _customerProductServiceMock.Setup(c => c.UpdateCustomerProduct(It.IsAny<CustomerProduct>()))
            .Returns(Task.CompletedTask);

        await _customerProductViewModelService.UpdatePersonalizedProduct(
            new CustomerModel.ProductModel { Id = "cp1", DisplayOrder = 7 });

        _customerProductServiceMock.Verify(
            c => c.UpdateCustomerProduct(It.Is<CustomerProduct>(x => x.DisplayOrder == 7)), Times.Once);
    }

    [TestMethod]
    public async Task DeletePersonalizedProduct_NotFound_Throws()
    {
        _customerProductServiceMock.Setup(c => c.GetCustomerProduct(It.IsAny<string>()))
            .ReturnsAsync((CustomerProduct)null);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _customerProductViewModelService.DeletePersonalizedProduct("missing"));
    }

    [TestMethod]
    public async Task PrepareProductPriceModel_MapsItems()
    {
        _customerProductServiceMock
            .Setup(c => c.GetProductsPriceByCustomer(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedList<CustomerProductPrice> { new() { Id = "pp1", ProductId = "p1", Price = 3 } });
        _productServiceMock.Setup(p => p.GetProductById("p1", false))
            .ReturnsAsync(new Product { Id = "p1", Name = "Prod 1" });

        var (items, _) = await _customerProductViewModelService.PrepareProductPriceModel("c1", 1, 10);

        var list = items.ToList();
        Assert.AreEqual(1, list.Count);
        Assert.AreEqual("Prod 1", list[0].ProductName);
        Assert.AreEqual(3, list[0].Price);
    }

    [TestMethod]
    public async Task InsertCustomerAddProductModel_Personalized_InsertsCustomerProduct()
    {
        _productServiceMock.Setup(p => p.GetProductById("p1", false))
            .ReturnsAsync(new Product { Id = "p1", Price = 5 });
        _customerProductServiceMock.Setup(c => c.GetCustomerProduct("c1", "p1")).ReturnsAsync((CustomerProduct)null);
        _customerProductServiceMock.Setup(c => c.InsertCustomerProduct(It.IsAny<CustomerProduct>()))
            .Returns(Task.CompletedTask);

        await _customerProductViewModelService.InsertCustomerAddProductModel("c1", true,
            new CustomerModel.AddProductModel { SelectedProductIds = new[] { "p1" } });

        _customerProductServiceMock.Verify(
            c => c.InsertCustomerProduct(It.Is<CustomerProduct>(x => x.CustomerId == "c1" && x.ProductId == "p1")),
            Times.Once);
    }

    [TestMethod]
    public async Task InsertCustomerAddProductModel_NotPersonalized_InsertsProductPrice()
    {
        _productServiceMock.Setup(p => p.GetProductById("p1", false))
            .ReturnsAsync(new Product { Id = "p1", Price = 5 });
        _customerProductServiceMock.Setup(c => c.GetPriceByCustomerProduct("c1", "p1"))
            .ReturnsAsync((double?)null);
        _customerProductServiceMock.Setup(c => c.InsertCustomerProductPrice(It.IsAny<CustomerProductPrice>()))
            .Returns(Task.CompletedTask);

        await _customerProductViewModelService.InsertCustomerAddProductModel("c1", false,
            new CustomerModel.AddProductModel { SelectedProductIds = new[] { "p1" } });

        _customerProductServiceMock.Verify(
            c => c.InsertCustomerProductPrice(
                It.Is<CustomerProductPrice>(x => x.CustomerId == "c1" && x.ProductId == "p1" && x.Price == 5)),
            Times.Once);
    }

    [TestMethod]
    public async Task PreparePersonalizedProducts_MapsItems()
    {
        _customerProductServiceMock
            .Setup(c => c.GetProductsByCustomer(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedList<CustomerProduct>
                { new() { Id = "cp1", ProductId = "p1", DisplayOrder = 2 } });
        _productServiceMock.Setup(p => p.GetProductById("p1", false))
            .ReturnsAsync(new Product { Id = "p1", Name = "Prod 1" });

        var (items, _) = await _customerProductViewModelService.PreparePersonalizedProducts("c1", 1, 10);

        var list = items.ToList();
        Assert.AreEqual(1, list.Count);
        Assert.AreEqual("Prod 1", list[0].ProductName);
        Assert.AreEqual(2, list[0].DisplayOrder);
    }
}
