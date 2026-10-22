using Grand.Business.Core.Interfaces.Catalog.Products;
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
public class BaseProductAttributeCombinationControllerTests : ProductControllerTestsBase<BaseProductAttributeCombinationControllerTests.TestProductAttributeCombinationController>
{
    // BaseProductAttributeCombinationController is abstract. This minimal subclass exists only so the actions under test
    // can be invoked directly.
    public class TestProductAttributeCombinationController(
        IProductViewModelService productViewModelService,
        IProductService productService,
        IInventoryManageService inventoryManageService,
        IRequestTranslationService translationService,
        IAdminDataScope<Product> scope)
        : BaseProductAttributeCombinationController(productViewModelService, productService, inventoryManageService, translationService, scope);

    protected override TestProductAttributeCombinationController CreateController() => new(
        _productViewModelServiceMock.Object,
        _productServiceMock.Object,
        _inventoryManageServiceMock.Object,
        _translationServiceMock.Object,
        _scopeMock.Object);

    // --- ProductAttributeCombinationList (POST) -------------------------------------------------------

    [TestMethod]
    public async Task ProductAttributeCombinationList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);

        var result = await _controller.ProductAttributeCombinationList(new DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductAttributeCombinationModel(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeCombinationList_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeCombinationModel(product))
            .ReturnsAsync(new List<ProductModel.ProductAttributeCombinationModel> { new() });

        var result = await _controller.ProductAttributeCombinationList(new DataSourceRequest(), "p1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as DataSourceResult;
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- ProductAttributeCombinationDelete -------------------------------------------------------------

    [TestMethod]
    public async Task ProductAttributeCombinationDelete_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);
        var attrServiceMock = new Mock<IProductAttributeService>();

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeCombinationDelete("c1", "missing", attrServiceMock.Object));
    }

    [TestMethod]
    public async Task ProductAttributeCombinationDelete_ScopeDeniesAccess_ReturnsErrorJson_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        product.ProductAttributeCombinations.Add(new ProductAttributeCombination { Id = "c1" });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var attrServiceMock = new Mock<IProductAttributeService>();

        var result = await _controller.ProductAttributeCombinationDelete("c1", "p1", attrServiceMock.Object);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        attrServiceMock.Verify(
            s => s.DeleteProductAttributeCombination(It.IsAny<ProductAttributeCombination>(), It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeCombinationDelete_ScopeGrantsAccess_CombinationMissing_Throws()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var attrServiceMock = new Mock<IProductAttributeService>();

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeCombinationDelete("missing-c", "p1", attrServiceMock.Object));
    }

    [TestMethod]
    public async Task ProductAttributeCombinationDelete_ScopeGrantsAccess_Deletes()
    {
        var product = new Product { Id = "p1", ManageInventoryMethodId = ManageInventoryMethod.DontManageStock };
        var combination = new ProductAttributeCombination { Id = "c1" };
        product.ProductAttributeCombinations.Add(combination);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var attrServiceMock = new Mock<IProductAttributeService>();

        var result = await _controller.ProductAttributeCombinationDelete("c1", "p1", attrServiceMock.Object);

        Assert.IsInstanceOfType<JsonResult>(result);
        attrServiceMock.Verify(s => s.DeleteProductAttributeCombination(combination, "p1"), Times.Once);
    }

    // --- AttributeCombinationPopup (GET) ---------------------------------------------------------------

    [TestMethod]
    public async Task AttributeCombinationPopupGet_ScopeDeniesAccess_ReturnsPermissionsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.AttributeCombinationPopup("p1", "c1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductAttributeCombinationModel(It.IsAny<Product>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task AttributeCombinationPopupGet_ScopeGrantsAccess_ReturnsView()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductAttributeCombinationModel { Id = "c1" };
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeCombinationModel(product, "c1"))
            .ReturnsAsync(model);

        var result = await _controller.AttributeCombinationPopup("p1", "c1");

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreSame(model, view.Model);
        _productViewModelServiceMock.Verify(s => s.PrepareAddProductAttributeCombinationModel(model, product),
            Times.Once);
    }

    // --- AttributeCombinationPopup (POST) --------------------------------------------------------------

    [TestMethod]
    public async Task AttributeCombinationPopupPost_MissingProduct_RedirectsToList()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);
        var model = new ProductAttributeCombinationModel();

        var result = await _controller.AttributeCombinationPopup("missing", model);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _scopeMock.Verify(s => s.HasAccess(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task AttributeCombinationPopupPost_ScopeDeniesAccess_ReturnsPermissionsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductAttributeCombinationModel();

        var result = await _controller.AttributeCombinationPopup("p1", model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.InsertOrUpdateProductAttributeCombinationPopup(It.IsAny<Product>(),
                It.IsAny<ProductAttributeCombinationModel>()), Times.Never);
    }

    [TestMethod]
    public async Task AttributeCombinationPopupPost_ScopeGrantsAccess_NoWarnings_ReturnsEmptyContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductAttributeCombinationModel();
        _productViewModelServiceMock.Setup(s => s.InsertOrUpdateProductAttributeCombinationPopup(product, model))
            .ReturnsAsync(new List<string>());

        var result = await _controller.AttributeCombinationPopup("p1", model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("", content.Content);
    }

    [TestMethod]
    public async Task AttributeCombinationPopupPost_ScopeGrantsAccess_WithWarnings_ReturnsViewWithWarnings()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductAttributeCombinationModel();
        _productViewModelServiceMock.Setup(s => s.InsertOrUpdateProductAttributeCombinationPopup(product, model))
            .ReturnsAsync(new List<string> { "warning" });

        var result = await _controller.AttributeCombinationPopup("p1", model);

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreSame(model, view.Model);
        CollectionAssert.Contains(model.Warnings.ToList(), "warning");
        _productViewModelServiceMock.Verify(s => s.PrepareAddProductAttributeCombinationModel(model, product),
            Times.Once);
    }

    // --- GenerateAllAttributeCombinations ---------------------------------------------------------------

    [TestMethod]
    public async Task GenerateAllAttributeCombinations_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.GenerateAllAttributeCombinations("missing"));
    }

    [TestMethod]
    public async Task GenerateAllAttributeCombinations_ScopeDeniesAccess_ReturnsErrorJson_DoesNotGenerate()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.GenerateAllAttributeCombinations("p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(s => s.GenerateAllAttributeCombinations(It.IsAny<Product>()),
            Times.Never);
    }

    [TestMethod]
    public async Task GenerateAllAttributeCombinations_ScopeGrantsAccess_Generates()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.GenerateAllAttributeCombinations("p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.GenerateAllAttributeCombinations(product), Times.Once);
    }

    // --- ClearAllAttributeCombinations -----------------------------------------------------------------

    [TestMethod]
    public async Task ClearAllAttributeCombinations_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ClearAllAttributeCombinations("missing"));
    }

    [TestMethod]
    public async Task ClearAllAttributeCombinations_ScopeDeniesAccess_ReturnsErrorJson_DoesNotClear()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ClearAllAttributeCombinations("p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(s => s.ClearAllAttributeCombinations(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task ClearAllAttributeCombinations_ScopeGrantsAccess_ValidModel_Clears()
    {
        var product = new Product
            { Id = "p1", ManageInventoryMethodId = ManageInventoryMethod.DontManageStock };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.ClearAllAttributeCombinations("p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.ClearAllAttributeCombinations(product), Times.Once);
    }

    [TestMethod]
    public async Task ClearAllAttributeCombinations_ScopeGrantsAccess_InvalidModelState_ReturnsErrorJson_DoesNotClear()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _controller.ModelState.AddModelError("x", "err");

        var result = await _controller.ClearAllAttributeCombinations("p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.ClearAllAttributeCombinations(It.IsAny<Product>()), Times.Never);
    }

    // --- ProductAttributeCombinationTierPriceList (POST) -----------------------------------------------

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductAttributeCombinationTierPriceList(new DataSourceRequest(), "p1", "c1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductAttributeCombinationTierPricesModel(It.IsAny<Product>(), It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceList_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeCombinationTierPricesModel(product, "c1"))
            .ReturnsAsync(new List<ProductModel.ProductAttributeCombinationTierPricesModel> { new() });

        var result = await _controller.ProductAttributeCombinationTierPriceList(new DataSourceRequest(), "p1", "c1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as DataSourceResult;
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- ProductAttributeCombinationTierPriceInsert -----------------------------------------------------

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceInsert_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeCombinationTierPriceInsert("missing", "c1",
                new ProductModel.ProductAttributeCombinationTierPricesModel()));
    }

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceInsert_ScopeDeniesAccess_ReturnsContent_DoesNotInsert()
    {
        var product = new Product { Id = "p1" };
        product.ProductAttributeCombinations.Add(new ProductAttributeCombination { Id = "c1" });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductAttributeCombinationTierPriceInsert("p1", "c1",
            new ProductModel.ProductAttributeCombinationTierPricesModel());

        Assert.IsInstanceOfType<ContentResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.InsertProductAttributeCombinationTierPricesModel(It.IsAny<Product>(),
                It.IsAny<ProductAttributeCombination>(),
                It.IsAny<ProductModel.ProductAttributeCombinationTierPricesModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceInsert_ScopeGrantsAccess_CombinationMissing_DoesNotInsert()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.ProductAttributeCombinationTierPriceInsert("p1", "missing-c",
            new ProductModel.ProductAttributeCombinationTierPricesModel());

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.InsertProductAttributeCombinationTierPricesModel(It.IsAny<Product>(),
                It.IsAny<ProductAttributeCombination>(),
                It.IsAny<ProductModel.ProductAttributeCombinationTierPricesModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceInsert_ScopeGrantsAccess_Inserts()
    {
        var product = new Product { Id = "p1" };
        var combination = new ProductAttributeCombination { Id = "c1" };
        product.ProductAttributeCombinations.Add(combination);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductAttributeCombinationTierPricesModel();

        var result = await _controller.ProductAttributeCombinationTierPriceInsert("p1", "c1", model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.InsertProductAttributeCombinationTierPricesModel(product, combination, model), Times.Once);
    }

    // --- ProductAttributeCombinationTierPriceUpdate -----------------------------------------------------

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceUpdate_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeCombinationTierPriceUpdate("missing", "c1",
                new ProductModel.ProductAttributeCombinationTierPricesModel()));
    }

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceUpdate_ScopeDeniesAccess_ReturnsContent_DoesNotUpdate()
    {
        var product = new Product { Id = "p1" };
        product.ProductAttributeCombinations.Add(new ProductAttributeCombination { Id = "c1" });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductAttributeCombinationTierPriceUpdate("p1", "c1",
            new ProductModel.ProductAttributeCombinationTierPricesModel());

        Assert.IsInstanceOfType<ContentResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductAttributeCombinationTierPricesModel(It.IsAny<Product>(),
                It.IsAny<ProductAttributeCombination>(),
                It.IsAny<ProductModel.ProductAttributeCombinationTierPricesModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceUpdate_ScopeGrantsAccess_Updates()
    {
        var product = new Product { Id = "p1" };
        var combination = new ProductAttributeCombination { Id = "c1" };
        product.ProductAttributeCombinations.Add(combination);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductAttributeCombinationTierPricesModel();

        var result = await _controller.ProductAttributeCombinationTierPriceUpdate("p1", "c1", model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductAttributeCombinationTierPricesModel(product, combination, model), Times.Once);
    }

    // --- ProductAttributeCombinationTierPriceDelete -----------------------------------------------------

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceDelete_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeCombinationTierPriceDelete("missing", "c1", "tp1"));
    }

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceDelete_ScopeDeniesAccess_ReturnsContent_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        product.ProductAttributeCombinations.Add(new ProductAttributeCombination { Id = "c1" });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductAttributeCombinationTierPriceDelete("p1", "c1", "tp1");

        Assert.IsInstanceOfType<ContentResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.DeleteProductAttributeCombinationTierPrices(It.IsAny<Product>(),
                It.IsAny<ProductAttributeCombination>(),
                It.IsAny<ProductCombinationTierPrices>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceDelete_ScopeGrantsAccess_TierPriceMissing_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        product.ProductAttributeCombinations.Add(new ProductAttributeCombination { Id = "c1" });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.ProductAttributeCombinationTierPriceDelete("p1", "c1", "missing-tp");

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.DeleteProductAttributeCombinationTierPrices(It.IsAny<Product>(),
                It.IsAny<ProductAttributeCombination>(),
                It.IsAny<ProductCombinationTierPrices>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeCombinationTierPriceDelete_ScopeGrantsAccess_Deletes()
    {
        var product = new Product { Id = "p1" };
        var combination = new ProductAttributeCombination { Id = "c1" };
        var tierPrice = new ProductCombinationTierPrices { Id = "tp1" };
        combination.TierPrices.Add(tierPrice);
        product.ProductAttributeCombinations.Add(combination);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.ProductAttributeCombinationTierPriceDelete("p1", "c1", "tp1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.DeleteProductAttributeCombinationTierPrices(product, combination, tierPrice), Times.Once);
    }
}
