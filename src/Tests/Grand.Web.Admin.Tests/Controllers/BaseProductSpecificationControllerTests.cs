using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain;
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
public class BaseProductSpecificationControllerTests : ProductControllerTestsBase<BaseProductSpecificationControllerTests.TestProductSpecificationController>
{
    // BaseProductSpecificationController is abstract. This minimal subclass exists only so the actions under test
    // can be invoked directly.
    public class TestProductSpecificationController(
        IProductViewModelService productViewModelService,
        IProductService productService,
        IRequestTranslationService translationService,
        IAdminDataScope<Product> scope)
        : BaseProductSpecificationController(productViewModelService, productService, translationService, scope);

    protected override TestProductSpecificationController CreateController() => new(
        _productViewModelServiceMock.Object,
        _productServiceMock.Object,
        _translationServiceMock.Object,
        _scopeMock.Object);

    // --- ProductSpecAttrList --------------------------------------------------------------------------

    [TestMethod]
    public async Task ProductSpecAttrList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);

        var result = await _controller.ProductSpecAttrList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.PrepareProductSpecificationAttributeModel(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductSpecAttrList_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);
        _productViewModelServiceMock.Setup(s => s.PrepareProductSpecificationAttributeModel(product))
            .ReturnsAsync(new List<ProductSpecificationAttributeModel> { new() { Id = "psa1" } });

        var result = await _controller.ProductSpecAttrList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        var json = (JsonResult)result;
        var grid = (Grand.Web.Common.DataSource.DataSourceResult)json.Value;
        Assert.AreEqual(1, grid.Total);
    }

    // --- ProductSpecAttrPopup (GET) -----------------------------------------------------------------

    [TestMethod]
    public async Task ProductSpecAttrPopupGet_ScopeDeniesAccess_ReturnsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductSpecAttrPopup(new Mock<ISpecificationAttributeService>().Object, "p1", "psa1");

        Assert.IsInstanceOfType<ContentResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductSpecificationAttributeModel(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductSpecAttrPopupGet_ScopeGrantsAccess_NewAttribute_ReturnsView()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var specAttrServiceMock = new Mock<ISpecificationAttributeService>();
        specAttrServiceMock.Setup(s => s.GetSpecificationAttributes(It.IsAny<string>(), 0, int.MaxValue))
            .ReturnsAsync(new PagedList<SpecificationAttribute>(new List<SpecificationAttribute>(), 0, int.MaxValue));

        var result = await _controller.ProductSpecAttrPopup(specAttrServiceMock.Object, "p1", "");

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        var model = view.Model as ProductModel.AddProductSpecificationAttributeModel;
        Assert.IsNotNull(model);
        Assert.IsTrue(model.ShowOnProductPage);
    }

    [TestMethod]
    public async Task ProductSpecAttrPopupGet_ScopeGrantsAccess_ExistingAttribute_ReturnsPopulatedView()
    {
        var psa = new ProductSpecificationAttribute { Id = "psa1", SpecificationAttributeOptionId = "opt1" };
        var product = new Product { Id = "p1" };
        product.ProductSpecificationAttributes.Add(psa);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var specAttrServiceMock = new Mock<ISpecificationAttributeService>();
        specAttrServiceMock.Setup(s => s.GetSpecificationAttributes(It.IsAny<string>(), 0, int.MaxValue))
            .ReturnsAsync(new PagedList<SpecificationAttribute>(new List<SpecificationAttribute>(), 0, int.MaxValue));

        var result = await _controller.ProductSpecAttrPopup(specAttrServiceMock.Object, "p1", "psa1");

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        var model = view.Model as ProductModel.AddProductSpecificationAttributeModel;
        Assert.IsNotNull(model);
        Assert.AreEqual("psa1", model.Id);
    }

    // --- ProductSpecAttrPopup (POST) ----------------------------------------------------------------

    [TestMethod]
    public async Task ProductSpecAttrPopupPost_ProductNotFound_ReturnsContent()
    {
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync((Product)null);
        var model = new ProductModel.AddProductSpecificationAttributeModel { ProductId = "p1", Id = "psa1" };

        var result = await _controller.ProductSpecAttrPopup(new Mock<ISpecificationAttributeService>().Object, model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductSpecificationAttributeModel(It.IsAny<ProductSpecificationAttribute>(),
                It.IsAny<ProductModel.AddProductSpecificationAttributeModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductSpecAttrPopupPost_ScopeDeniesAccess_ReturnsContent_DoesNotInsertOrUpdate()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.AddProductSpecificationAttributeModel { ProductId = "p1", Id = "psa1" };

        // Regression guard: Vendor's original ProductSpecAttrPopup(POST) had no access check at all,
        // letting any vendor add/edit specification attributes on another vendor's product by posting
        // its id.
        var result = await _controller.ProductSpecAttrPopup(new Mock<ISpecificationAttributeService>().Object, model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.InsertProductSpecificationAttributeModel(It.IsAny<ProductModel.AddProductSpecificationAttributeModel>(),
                It.IsAny<Product>()), Times.Never);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductSpecificationAttributeModel(It.IsAny<ProductSpecificationAttribute>(),
                It.IsAny<ProductModel.AddProductSpecificationAttributeModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductSpecAttrPopupPost_ScopeGrantsAccess_NewAttribute_Inserts()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.AddProductSpecificationAttributeModel { ProductId = "p1", Id = "psa-new" };

        var result = await _controller.ProductSpecAttrPopup(new Mock<ISpecificationAttributeService>().Object, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.InsertProductSpecificationAttributeModel(model, product), Times.Once);
    }

    [TestMethod]
    public async Task ProductSpecAttrPopupPost_ScopeGrantsAccess_ExistingAttribute_Updates()
    {
        var psa = new ProductSpecificationAttribute { Id = "psa1" };
        var product = new Product { Id = "p1" };
        product.ProductSpecificationAttributes.Add(psa);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.AddProductSpecificationAttributeModel { ProductId = "p1", Id = "psa1" };

        var result = await _controller.ProductSpecAttrPopup(new Mock<ISpecificationAttributeService>().Object, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.UpdateProductSpecificationAttributeModel(psa, model), Times.Once);
    }

    [TestMethod]
    public async Task ProductSpecAttrPopupPost_InvalidModelState_ReturnsView_DoesNotAccessProduct()
    {
        var model = new ProductModel.AddProductSpecificationAttributeModel { ProductId = "p1", Id = "psa1" };
        _controller.ModelState.AddModelError("x", "error");
        var specAttrServiceMock = new Mock<ISpecificationAttributeService>();
        specAttrServiceMock.Setup(s => s.GetSpecificationAttributes(It.IsAny<string>(), 0, int.MaxValue))
            .ReturnsAsync(new PagedList<SpecificationAttribute>(new List<SpecificationAttribute>(), 0, int.MaxValue));

        var result = await _controller.ProductSpecAttrPopup(specAttrServiceMock.Object, model);

        Assert.IsInstanceOfType<ViewResult>(result);
        _productServiceMock.Verify(p => p.GetProductById(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    // --- ProductSpecAttrDelete ------------------------------------------------------------------------

    [TestMethod]
    public async Task ProductSpecAttrDelete_ProductNotFound_ReturnsContent()
    {
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync((Product)null);
        var model = new ProductSpecificationAttributeModel { ProductId = "p1", Id = "psa1" };

        var result = await _controller.ProductSpecAttrDelete(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.DeleteProductSpecificationAttribute(It.IsAny<Product>(), It.IsAny<ProductSpecificationAttribute>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductSpecAttrDelete_ScopeDeniesAccess_ReturnsContent_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductSpecificationAttributeModel { ProductId = "p1", Id = "psa1" };

        // Regression guard: Vendor's original ProductSpecAttrDelete had no access check at all, letting
        // any vendor delete another vendor's specification attribute mapping by posting its
        // productId/model.Id.
        var result = await _controller.ProductSpecAttrDelete(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.DeleteProductSpecificationAttribute(It.IsAny<Product>(), It.IsAny<ProductSpecificationAttribute>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductSpecAttrDelete_ScopeGrantsAccess_ValidModel_Deletes()
    {
        var psa = new ProductSpecificationAttribute { Id = "psa1" };
        var product = new Product { Id = "p1" };
        product.ProductSpecificationAttributes.Add(psa);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductSpecificationAttributeModel { ProductId = "p1", Id = "psa1" };

        var result = await _controller.ProductSpecAttrDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productViewModelServiceMock.Verify(s => s.DeleteProductSpecificationAttribute(product, psa), Times.Once);
    }

    [TestMethod]
    public async Task ProductSpecAttrDelete_ScopeGrantsAccess_AttributeNotFound_Throws()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductSpecificationAttributeModel { ProductId = "p1", Id = "missing" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.ProductSpecAttrDelete(model));
    }

    [TestMethod]
    public async Task ProductSpecAttrDelete_InvalidModelState_ReturnsKendoGridError_DoesNotAccessProduct()
    {
        var model = new ProductSpecificationAttributeModel { ProductId = "p1", Id = "psa1" };
        _controller.ModelState.AddModelError("x", "error");

        var result = await _controller.ProductSpecAttrDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productServiceMock.Verify(p => p.GetProductById(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }
}
