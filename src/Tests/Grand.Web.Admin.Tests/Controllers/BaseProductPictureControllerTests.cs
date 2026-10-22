using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Media;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq.Expressions;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseProductPictureControllerTests : ProductControllerTestsBase<BaseProductPictureControllerTests.TestProductPictureController>
{
    // BaseProductPictureController is abstract. This minimal subclass exists only so the actions under test
    // can be invoked directly.
    public class TestProductPictureController(
        IProductViewModelService productViewModelService,
        IProductService productService,
        ILanguageService languageService,
        IRequestTranslationService translationService,
        IRequestPermissionService permissionService,
        IAdminDataScope<Product> scope)
        : BaseProductPictureController(productViewModelService, productService, languageService, translationService, permissionService, scope);

    protected override TestProductPictureController CreateController() => new(
        _productViewModelServiceMock.Object,
        _productServiceMock.Object,
        _languageServiceMock.Object,
        _translationServiceMock.Object,
        _permissionServiceMock.Object,
        _scopeMock.Object);

    // --- ProductPictureList --------------------------------------------------------------------------

    [TestMethod]
    public async Task ProductPictureList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);

        var result = await _controller.ProductPictureList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(s => s.PrepareProductPicturesModel(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductPictureList_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);
        _productViewModelServiceMock.Setup(s => s.PrepareProductPicturesModel(product))
            .ReturnsAsync(new List<ProductModel.ProductPictureModel> { new() { Id = "pic1" } });

        var result = await _controller.ProductPictureList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- ProductPicturePopup (GET) -------------------------------------------------------------------

    [TestMethod]
    public async Task ProductPicturePopupGet_ProductNotFound_ReturnsContent()
    {
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync((Product)null);

        var result = await _controller.ProductPicturePopup("p1", "pic1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("Product not exist", content.Content);
        _scopeMock.Verify(s => s.HasAccess(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductPicturePopupGet_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductPicturePopup("p1", "pic1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
    }

    [TestMethod]
    public async Task ProductPicturePopupGet_ScopeGrantsAccess_PictureNotFound_ReturnsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.ProductPicturePopup("p1", "pic1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("Product picture not exist", content.Content);
    }

    [TestMethod]
    public async Task ProductPicturePopupGet_ScopeGrantsAccess_PictureFound_ReturnsView()
    {
        var pp = new ProductPicture { Id = "pic1" };
        var product = new Product { Id = "p1" };
        product.ProductPictures.Add(pp);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductPictureModel { Id = "pic1" };
        _productViewModelServiceMock.Setup(s => s.PrepareProductPictureModel(product, pp))
            .ReturnsAsync((model, (Picture)null));

        var result = await _controller.ProductPicturePopup("p1", "pic1");

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreSame(model, view.Model);
    }

    // --- ProductPicturePopup (POST) ------------------------------------------------------------------

    [TestMethod]
    public async Task ProductPicturePopupPost_ProductNotFound_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync((Product)null);
        var model = new ProductModel.ProductPictureModel { ProductId = "p1", Id = "pic1" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.ProductPicturePopup(model));
    }

    [TestMethod]
    public async Task ProductPicturePopupPost_ScopeDeniesAccess_Throws_DoesNotUpdate()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductPictureModel { ProductId = "p1", Id = "pic1" };

        // Regression guard: Vendor's original ProductPicturePopup(POST) had no access check at all,
        // letting any vendor rename/re-alt-text another vendor's product picture by posting its
        // productId/model.Id.
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.ProductPicturePopup(model));
        _productViewModelServiceMock.Verify(s => s.UpdateProductPicture(It.IsAny<ProductModel.ProductPictureModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductPicturePopupPost_ScopeGrantsAccess_PictureNotFound_Throws()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductPictureModel { ProductId = "p1", Id = "pic1" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.ProductPicturePopup(model));
    }

    [TestMethod]
    public async Task ProductPicturePopupPost_ScopeGrantsAccess_ValidModel_Updates()
    {
        var pp = new ProductPicture { Id = "pic1" };
        var product = new Product { Id = "p1" };
        product.ProductPictures.Add(pp);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductPictureModel { ProductId = "p1", Id = "pic1" };

        var result = await _controller.ProductPicturePopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("", content.Content);
        _productViewModelServiceMock.Verify(s => s.UpdateProductPicture(model), Times.Once);
    }

    [TestMethod]
    public async Task ProductPicturePopupPost_InvalidModelState_ReturnsView()
    {
        var model = new ProductModel.ProductPictureModel { ProductId = "p1", Id = "pic1" };
        _controller.ModelState.AddModelError("x", "error");

        var result = await _controller.ProductPicturePopup(model);

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreSame(model, view.Model);
        _productServiceMock.Verify(p => p.GetProductById(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    // --- ProductPictureDelete -------------------------------------------------------------------------

    [TestMethod]
    public async Task ProductPictureDelete_ScopeDeniesAccess_ReturnsErrorJson_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductPictureModel { ProductId = "p1", Id = "pic1" };

        // Regression guard: Vendor's original ProductPictureDelete had no access check at all, letting
        // any vendor delete another vendor's product picture by posting its productId/model.Id.
        var result = await _controller.ProductPictureDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(s => s.DeleteProductPicture(It.IsAny<ProductModel.ProductPictureModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductPictureDelete_ScopeGrantsAccess_ValidModel_Deletes()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductPictureModel { ProductId = "p1", Id = "pic1" };

        var result = await _controller.ProductPictureDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.DeleteProductPicture(model), Times.Once);
    }

    [TestMethod]
    public async Task ProductPictureDelete_ScopeGrantsAccess_InvalidModelState_ReturnsKendoGridError_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductPictureModel { ProductId = "p1", Id = "pic1" };
        _controller.ModelState.AddModelError("x", "error");

        var result = await _controller.ProductPictureDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.DeleteProductPicture(It.IsAny<ProductModel.ProductPictureModel>()), Times.Never);
    }
}
