using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq.Expressions;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseProductPriceControllerTests : ProductControllerTestsBase<BaseProductPriceControllerTests.TestProductPriceController>
{
    // BaseProductPriceController is abstract. This minimal subclass exists only so the actions under test
    // can be invoked directly.
    public class TestProductPriceController(
        IProductPriceViewModelService productPriceViewModelService,
        IProductService productService,
        IRequestTranslationService translationService,
        IDateTimeService dateTimeService,
        IAdminDataScope<Product> scope)
        : BaseProductPriceController(productPriceViewModelService, productService, translationService, dateTimeService, scope);

    protected override TestProductPriceController CreateController() => new(
        _productPriceViewModelServiceMock.Object,
        _productServiceMock.Object,
        _translationServiceMock.Object,
        _dateTimeServiceMock.Object,
        _scopeMock.Object);

    // --- ProductPriceList -------------------------------------------------------------------------
    // HasAccess (strict), not CanView: mirrors Store's CanAccessProduct check on this action. Applying it
    // uniformly also closes real gaps on the mutate actions below: Store's original checked access only on
    // List/Insert (Update/Delete had no check at all), and Vendor's original checked access only on List
    // (Insert/Update/Delete had no check at all).

    [TestMethod]
    public async Task ProductPriceList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);

        var result = await _controller.ProductPriceList(
            new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
    }

    [TestMethod]
    public async Task ProductPriceList_ScopeDeniesAccess_UsesScopeResourceKeyPrefix()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);
        _scopeMock.Setup(s => s.ResourceKeyPrefix).Returns("Vendor");

        var result = await _controller.ProductPriceList(
            new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Vendor.Catalog.Products.Permissions"), Times.Once);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Never);
    }

    [TestMethod]
    public async Task ProductPriceList_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        product.ProductPrices.Add(new ProductPrice { Id = "pp1", CurrencyCode = "EUR", Price = 9.99 });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);

        var result = await _controller.ProductPriceList(
            new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- ProductPriceInsert -----------------------------------------------------------------------

    [TestMethod]
    public async Task ProductPriceInsert_ScopeDeniesAccess_ReturnsErrorJson_DoesNotInsert()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductPriceModel { ProductId = "p1", CurrencyCode = "EUR", Price = 9.99 };

        var result = await _controller.ProductPriceInsert(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productServiceMock.Verify(s => s.InsertProductPrice(It.IsAny<ProductPrice>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductPriceInsert_ScopeGrantsAccess_ValidModel_Inserts()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductPriceModel { ProductId = "p1", CurrencyCode = "EUR", Price = 9.99 };

        var result = await _controller.ProductPriceInsert(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productServiceMock.Verify(s => s.InsertProductPrice(It.Is<ProductPrice>(
            pp => pp.ProductId == "p1" && pp.CurrencyCode == "EUR" && pp.Price == 9.99)), Times.Once);
    }

    // --- ProductPriceUpdate -----------------------------------------------------------------------

    [TestMethod]
    public async Task ProductPriceUpdate_ScopeDeniesAccess_ReturnsErrorJson_DoesNotUpdate()
    {
        var product = new Product { Id = "p1" };
        product.ProductPrices.Add(new ProductPrice { Id = "pp1", CurrencyCode = "EUR", Price = 9.99 });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductPriceModel { Id = "pp1", ProductId = "p1", CurrencyCode = "USD", Price = 19.99 };

        var result = await _controller.ProductPriceUpdate(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productServiceMock.Verify(s => s.UpdateProductPrice(It.IsAny<ProductPrice>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductPriceUpdate_ScopeGrantsAccess_ValidModel_Updates()
    {
        var product = new Product { Id = "p1" };
        product.ProductPrices.Add(new ProductPrice { Id = "pp1", CurrencyCode = "EUR", Price = 9.99 });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductPriceModel { Id = "pp1", ProductId = "p1", CurrencyCode = "USD", Price = 19.99 };

        var result = await _controller.ProductPriceUpdate(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productServiceMock.Verify(s => s.UpdateProductPrice(It.Is<ProductPrice>(
            pp => pp.Id == "pp1" && pp.CurrencyCode == "USD" && pp.Price == 19.99 && pp.ProductId == "p1")), Times.Once);
    }

    // --- ProductPriceDelete -----------------------------------------------------------------------

    [TestMethod]
    public async Task ProductPriceDelete_ScopeDeniesAccess_ReturnsErrorJson_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        product.ProductPrices.Add(new ProductPrice { Id = "pp1", CurrencyCode = "EUR", Price = 9.99 });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductPriceModel { Id = "pp1", ProductId = "p1" };

        var result = await _controller.ProductPriceDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productServiceMock.Verify(s => s.DeleteProductPrice(It.IsAny<ProductPrice>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductPriceDelete_ScopeGrantsAccess_ValidModel_Deletes()
    {
        var product = new Product { Id = "p1" };
        product.ProductPrices.Add(new ProductPrice { Id = "pp1", CurrencyCode = "EUR", Price = 9.99 });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductPriceModel { Id = "pp1", ProductId = "p1" };

        var result = await _controller.ProductPriceDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productServiceMock.Verify(s => s.DeleteProductPrice(It.Is<ProductPrice>(
            pp => pp.Id == "pp1" && pp.ProductId == "p1")), Times.Once);
    }

    // --- TierPriceList ----------------------------------------------------------------------------
    // HasAccess applied uniformly on every action in this region. Vendor's original checked ownership
    // only on List and TierPriceEditPopup(GET); TierPriceCreatePopup(POST), TierPriceEditPopup(POST) and
    // TierPriceDelete had NO ownership check at all, letting a vendor create/update/delete a tier price on
    // any product by id. Store's original never checked TierPriceEditPopup(GET) either.

    [TestMethod]
    public async Task TierPriceList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);

        var result = await _controller.TierPriceList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productPriceViewModelServiceMock.Verify(
            s => s.PrepareTierPriceModel(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task TierPriceList_ScopeDeniesAccess_UsesScopeResourceKeyPrefix()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);
        _scopeMock.Setup(s => s.ResourceKeyPrefix).Returns("Vendor");

        var result = await _controller.TierPriceList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Vendor.Catalog.Products.Permissions"), Times.Once);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Never);
    }

    [TestMethod]
    public async Task TierPriceList_ScopeGrantsAccess_ReturnsGrid()
    {
        // PrepareTierPriceModel(product) now reads scope internally (Task 9).
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);
        var tierPrices = new List<ProductModel.TierPriceModel> { new() { Id = "tp1" } };
        _productPriceViewModelServiceMock.Setup(s => s.PrepareTierPriceModel(product)).ReturnsAsync(tierPrices);

        var result = await _controller.TierPriceList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- TierPriceCreatePopup (GET) ----------------------------------------------------------------

    [TestMethod]
    public async Task TierPriceCreatePopup_Get_PreparesModel()
    {
        // PrepareTierPriceModel(model) now reads scope internally (Task 9).
        var result = await _controller.TierPriceCreatePopup("p1");

        Assert.IsInstanceOfType<ViewResult>(result);
        _productPriceViewModelServiceMock.Verify(
            s => s.PrepareTierPriceModel(It.Is<ProductModel.TierPriceModel>(m => m.ProductId == "p1")),
            Times.Once);
    }

    // --- TierPriceCreatePopup (POST) ---------------------------------------------------------------

    [TestMethod]
    public async Task TierPriceCreatePopup_Post_ScopeDeniesAccess_DoesNotInsert()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.TierPriceModel { ProductId = "p1" };

        var result = await _controller.TierPriceCreatePopup(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productServiceMock.Verify(s => s.InsertTierPrice(It.IsAny<TierPrice>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task TierPriceCreatePopup_Post_ScopeGrantsAccess_ValidModel_Inserts()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.TierPriceModel { ProductId = "p1" };

        var result = await _controller.TierPriceCreatePopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _productServiceMock.Verify(
            s => s.InsertTierPrice(It.IsAny<TierPrice>(), "p1"), Times.Once);
    }

    [TestMethod]
    public async Task TierPriceCreatePopup_Post_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);
        var model = new ProductModel.TierPriceModel { ProductId = "missing" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.TierPriceCreatePopup(model));
    }

    // --- TierPriceEditPopup (GET) ------------------------------------------------------------------
    // HasAccess added here: Store's original had no ownership check on this GET action at all.

    [TestMethod]
    public async Task TierPriceEditPopup_Get_ScopeDeniesAccess_ReturnsNotYourProductContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.TierPriceEditPopup("tp1", "p1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("This is not your product", content.Content);
        _productPriceViewModelServiceMock.Verify(
            s => s.PrepareTierPriceModel(It.IsAny<ProductModel.TierPriceModel>()), Times.Never);
    }

    [TestMethod]
    public async Task TierPriceEditPopup_Get_ScopeGrantsAccess_TierPriceMissing_ReturnsEmptyContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.TierPriceEditPopup("missing-tp", "p1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("Empty tier price", content.Content);
    }

    [TestMethod]
    public async Task TierPriceEditPopup_Get_ScopeGrantsAccess_PreparesModel()
    {
        // PrepareTierPriceModel(model) now reads scope internally (Task 9).
        var product = new Product { Id = "p1" };
        product.TierPrices.Add(new TierPrice { Id = "tp1" });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.TierPriceEditPopup("tp1", "p1");

        Assert.IsInstanceOfType<ViewResult>(result);
        _productPriceViewModelServiceMock.Verify(
            s => s.PrepareTierPriceModel(It.Is<ProductModel.TierPriceModel>(m => m.ProductId == "p1")),
            Times.Once);
    }

    [TestMethod]
    public async Task TierPriceEditPopup_Get_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.TierPriceEditPopup("tp1", "missing"));
    }

    // --- TierPriceEditPopup (POST) -----------------------------------------------------------------

    [TestMethod]
    public async Task TierPriceEditPopup_Post_ScopeDeniesAccess_DoesNotUpdate()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.TierPriceModel { Id = "tp1", ProductId = "p1" };

        var result = await _controller.TierPriceEditPopup("p1", model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productServiceMock.Verify(s => s.UpdateTierPrice(It.IsAny<TierPrice>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task TierPriceEditPopup_Post_ScopeGrantsAccess_ValidModel_Updates()
    {
        var product = new Product { Id = "p1" };
        product.TierPrices.Add(new TierPrice { Id = "tp1" });
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.TierPriceModel { Id = "tp1", ProductId = "p1" };

        var result = await _controller.TierPriceEditPopup("p1", model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _productServiceMock.Verify(s => s.UpdateTierPrice(It.IsAny<TierPrice>(), "p1"), Times.Once);
    }

    // --- TierPriceDelete ----------------------------------------------------------------------------

    [TestMethod]
    public async Task TierPriceDelete_ScopeDeniesAccess_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.TierPriceDeleteModel("tp1", "p1");

        var result = await _controller.TierPriceDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productServiceMock.Verify(s => s.DeleteTierPrice(It.IsAny<TierPrice>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task TierPriceDelete_ScopeGrantsAccess_ValidModel_Deletes()
    {
        var product = new Product { Id = "p1" };
        product.TierPrices.Add(new TierPrice { Id = "tp1" });
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.TierPriceDeleteModel("tp1", "p1");

        var result = await _controller.TierPriceDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productServiceMock.Verify(s => s.DeleteTierPrice(It.IsAny<TierPrice>(), "p1"), Times.Once);
    }

    [TestMethod]
    public async Task TierPriceDelete_ScopeGrantsAccess_TierPriceMissing_Throws()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", true)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.TierPriceDeleteModel("missing-tp", "p1");

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.TierPriceDelete(model));
    }
}
