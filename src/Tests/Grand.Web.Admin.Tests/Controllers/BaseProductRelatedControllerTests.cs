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
public class BaseProductRelatedControllerTests : ProductControllerTestsBase<BaseProductRelatedControllerTests.TestProductRelatedController>
{
    // BaseProductRelatedController is abstract. This minimal subclass exists only so the actions under test
    // can be invoked directly.
    public class TestProductRelatedController(
        IProductAddPopupViewModelService productAddPopupViewModelService,
        IProductRelatedViewModelService productRelatedViewModelService,
        IProductService productService,
        IRequestTranslationService translationService,
        IAdminDataScope<Product> scope)
        : BaseProductRelatedController(productAddPopupViewModelService, productRelatedViewModelService, productService, translationService, scope);

    protected override TestProductRelatedController CreateController() => new(
        _productAddPopupViewModelServiceMock.Object,
        _productRelatedViewModelServiceMock.Object,
        _productServiceMock.Object,
        _translationServiceMock.Object,
        _scopeMock.Object);

    // --- RelatedProductList ------------------------------------------------------------------------
    // HasAccess (strict), not CanView: same shape as ProductCategoryList/ProductCollectionList above.

    [TestMethod]
    public async Task RelatedProductList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);

        var result = await _controller.RelatedProductList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
    }

    [TestMethod]
    public async Task RelatedProductList_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        product.RelatedProducts.Add(new RelatedProduct { Id = "r1", ProductId2 = "p2", DisplayOrder = 0 });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _productServiceMock.Setup(p => p.GetProductById("p2", false)).ReturnsAsync(new Product { Id = "p2", Name = "Second" });
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);

        var result = await _controller.RelatedProductList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- RelatedProductUpdate ----------------------------------------------------------------------

    [TestMethod]
    public async Task RelatedProductUpdate_ScopeDeniesAccess_ReturnsErrorJson_DoesNotUpdate()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.RelatedProductModel { ProductId1 = "p1" };

        var result = await _controller.RelatedProductUpdate(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productRelatedViewModelServiceMock.Verify(s => s.UpdateRelatedProductModel(It.IsAny<ProductModel.RelatedProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task RelatedProductUpdate_ScopeGrantsAccess_ValidModel_Updates()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.RelatedProductModel { ProductId1 = "p1" };

        var result = await _controller.RelatedProductUpdate(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productRelatedViewModelServiceMock.Verify(s => s.UpdateRelatedProductModel(model), Times.Once);
    }

    // --- RelatedProductDelete ----------------------------------------------------------------------

    [TestMethod]
    public async Task RelatedProductDelete_ScopeDeniesAccess_ReturnsErrorJson_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.RelatedProductModel { ProductId1 = "p1" };

        var result = await _controller.RelatedProductDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productRelatedViewModelServiceMock.Verify(s => s.DeleteRelatedProductModel(It.IsAny<ProductModel.RelatedProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task RelatedProductDelete_ScopeGrantsAccess_ValidModel_Deletes()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.RelatedProductModel { ProductId1 = "p1" };

        var result = await _controller.RelatedProductDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productRelatedViewModelServiceMock.Verify(s => s.DeleteRelatedProductModel(model), Times.Once);
    }

    // --- RelatedProductAddPopup (GET) --------------------------------------------------------------

    [TestMethod]
    public async Task RelatedProductAddPopupGet_CallsPrepareRelatedProductModel()
    {
        // PrepareRelatedProductModel() now reads scope internally (Task 9).
        _productAddPopupViewModelServiceMock.Setup(s => s.PrepareRelatedProductModel())
            .ReturnsAsync(new ProductModel.AddRelatedProductModel());

        var result = await _controller.RelatedProductAddPopup("p1") as ViewResult;

        Assert.IsNotNull(result);
        var model = result.Model as ProductModel.AddRelatedProductModel;
        Assert.IsNotNull(model);
        Assert.AreEqual("p1", model.ProductId);
        _productAddPopupViewModelServiceMock.Verify(s => s.PrepareRelatedProductModel(), Times.Once);
    }

    // --- RelatedProductAddPopupList -----------------------------------------------------------------

    [TestMethod]
    public async Task RelatedProductAddPopupList_UsesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _productAddPopupViewModelServiceMock
            .Setup(s => s.PrepareProductModel(It.IsAny<ProductModel.AddRelatedProductModel>(), 0, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var model = new ProductModel.AddRelatedProductModel();
        var result = await _controller.RelatedProductAddPopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 0, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("store-1", model.SearchStoreId);
    }

    [TestMethod]
    public async Task RelatedProductAddPopupList_NoDefaultStoreId_DoesNotOverrideModelSearchStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _productAddPopupViewModelServiceMock
            .Setup(s => s.PrepareProductModel(It.IsAny<ProductModel.AddRelatedProductModel>(), 0, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var model = new ProductModel.AddRelatedProductModel { SearchStoreId = "explicit" };
        var result = await _controller.RelatedProductAddPopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 0, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("explicit", model.SearchStoreId);
    }

    // --- RelatedProductAddPopup (POST) --------------------------------------------------------------
    // HasAccess (strict): closes a real gap - Vendor's original RelatedProductAddPopup(POST) had no
    // ownership check at all, letting any vendor attach related-product mappings onto another vendor's
    // product by posting its id.

    [TestMethod]
    public async Task RelatedProductAddPopupPost_ScopeDeniesAccess_ReturnsContentMessage_DoesNotInsert()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.AddRelatedProductModel { ProductId = "p1", SelectedProductIds = ["p2"] };

        var result = await _controller.RelatedProductAddPopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productRelatedViewModelServiceMock.Verify(s => s.InsertRelatedProductModel(It.IsAny<ProductModel.AddRelatedProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task RelatedProductAddPopupPost_ScopeGrantsAccess_ValidModel_Inserts()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.AddRelatedProductModel { ProductId = "p1", SelectedProductIds = ["p2"] };

        var result = await _controller.RelatedProductAddPopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("", content.Content);
        _productRelatedViewModelServiceMock.Verify(s => s.InsertRelatedProductModel(model), Times.Once);
    }

    [TestMethod]
    public async Task RelatedProductAddPopupPost_ScopeGrantsAccess_InvalidModel_ReturnsViewWithRepreparedModel()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var reprepared = new ProductModel.AddRelatedProductModel();
        _productAddPopupViewModelServiceMock.Setup(s => s.PrepareRelatedProductModel()).ReturnsAsync(reprepared);
        var model = new ProductModel.AddRelatedProductModel { ProductId = "p1" };
        _controller.ModelState.AddModelError("x", "error");

        var result = await _controller.RelatedProductAddPopup(model);

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreSame(reprepared, view.Model);
        _productRelatedViewModelServiceMock.Verify(s => s.InsertRelatedProductModel(It.IsAny<ProductModel.AddRelatedProductModel>()), Times.Never);
    }

    // --- SimilarProductList ------------------------------------------------------------------------
    // HasAccess (strict), not CanView: same shape as RelatedProductList above.

    [TestMethod]
    public async Task SimilarProductList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);

        var result = await _controller.SimilarProductList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
    }

    [TestMethod]
    public async Task SimilarProductList_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        product.SimilarProducts.Add(new SimilarProduct { Id = "r1", ProductId2 = "p2", DisplayOrder = 0 });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _productServiceMock.Setup(p => p.GetProductById("p2", false)).ReturnsAsync(new Product { Id = "p2", Name = "Second" });
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);

        var result = await _controller.SimilarProductList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- SimilarProductUpdate ----------------------------------------------------------------------

    [TestMethod]
    public async Task SimilarProductUpdate_ScopeDeniesAccess_ReturnsErrorJson_DoesNotUpdate()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.SimilarProductModel { ProductId1 = "p1" };

        var result = await _controller.SimilarProductUpdate(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productRelatedViewModelServiceMock.Verify(s => s.UpdateSimilarProductModel(It.IsAny<ProductModel.SimilarProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task SimilarProductUpdate_ScopeGrantsAccess_ValidModel_Updates()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.SimilarProductModel { ProductId1 = "p1" };

        var result = await _controller.SimilarProductUpdate(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productRelatedViewModelServiceMock.Verify(s => s.UpdateSimilarProductModel(model), Times.Once);
    }

    // --- SimilarProductDelete ----------------------------------------------------------------------

    [TestMethod]
    public async Task SimilarProductDelete_ScopeDeniesAccess_ReturnsErrorJson_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.SimilarProductModel { ProductId1 = "p1" };

        var result = await _controller.SimilarProductDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productRelatedViewModelServiceMock.Verify(s => s.DeleteSimilarProductModel(It.IsAny<ProductModel.SimilarProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task SimilarProductDelete_ScopeGrantsAccess_ValidModel_Deletes()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.SimilarProductModel { ProductId1 = "p1" };

        var result = await _controller.SimilarProductDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productRelatedViewModelServiceMock.Verify(s => s.DeleteSimilarProductModel(model), Times.Once);
    }

    // --- SimilarProductAddPopup (GET) --------------------------------------------------------------

    [TestMethod]
    public async Task SimilarProductAddPopupGet_CallsPrepareSimilarProductModel()
    {
        // PrepareSimilarProductModel() now reads scope internally (Task 9).
        _productAddPopupViewModelServiceMock.Setup(s => s.PrepareSimilarProductModel())
            .ReturnsAsync(new ProductModel.AddSimilarProductModel());

        var result = await _controller.SimilarProductAddPopup("p1") as ViewResult;

        Assert.IsNotNull(result);
        var model = result.Model as ProductModel.AddSimilarProductModel;
        Assert.IsNotNull(model);
        Assert.AreEqual("p1", model.ProductId);
        _productAddPopupViewModelServiceMock.Verify(s => s.PrepareSimilarProductModel(), Times.Once);
    }

    // --- SimilarProductAddPopupList -----------------------------------------------------------------

    [TestMethod]
    public async Task SimilarProductAddPopupList_UsesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _productAddPopupViewModelServiceMock
            .Setup(s => s.PrepareProductModel(It.IsAny<ProductModel.AddSimilarProductModel>(), 0, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var model = new ProductModel.AddSimilarProductModel();
        var result = await _controller.SimilarProductAddPopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 0, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("store-1", model.SearchStoreId);
    }

    [TestMethod]
    public async Task SimilarProductAddPopupList_NoDefaultStoreId_DoesNotOverrideModelSearchStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _productAddPopupViewModelServiceMock
            .Setup(s => s.PrepareProductModel(It.IsAny<ProductModel.AddSimilarProductModel>(), 0, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var model = new ProductModel.AddSimilarProductModel { SearchStoreId = "explicit" };
        var result = await _controller.SimilarProductAddPopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 0, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("explicit", model.SearchStoreId);
    }

    // --- SimilarProductAddPopup (POST) --------------------------------------------------------------
    // HasAccess (strict): closes a real gap - Vendor's original SimilarProductAddPopup(POST) had no
    // ownership check at all, letting any vendor attach similar-product mappings onto another vendor's
    // product by posting its id.

    [TestMethod]
    public async Task SimilarProductAddPopupPost_ScopeDeniesAccess_ReturnsContentMessage_DoesNotInsert()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.AddSimilarProductModel { ProductId = "p1", SelectedProductIds = ["p2"] };

        var result = await _controller.SimilarProductAddPopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productRelatedViewModelServiceMock.Verify(s => s.InsertSimilarProductModel(It.IsAny<ProductModel.AddSimilarProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task SimilarProductAddPopupPost_ScopeGrantsAccess_ValidModel_Inserts()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.AddSimilarProductModel { ProductId = "p1", SelectedProductIds = ["p2"] };

        var result = await _controller.SimilarProductAddPopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("", content.Content);
        _productRelatedViewModelServiceMock.Verify(s => s.InsertSimilarProductModel(model), Times.Once);
    }

    [TestMethod]
    public async Task SimilarProductAddPopupPost_ScopeGrantsAccess_InvalidModel_ReturnsViewWithRepreparedModel()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var reprepared = new ProductModel.AddSimilarProductModel();
        _productAddPopupViewModelServiceMock.Setup(s => s.PrepareSimilarProductModel()).ReturnsAsync(reprepared);
        var model = new ProductModel.AddSimilarProductModel { ProductId = "p1" };
        _controller.ModelState.AddModelError("x", "error");

        var result = await _controller.SimilarProductAddPopup(model);

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreSame(reprepared, view.Model);
        _productRelatedViewModelServiceMock.Verify(s => s.InsertSimilarProductModel(It.IsAny<ProductModel.AddSimilarProductModel>()), Times.Never);
    }

    // --- CrossSellProductList ---------------------------------------------------------------------
    // HasAccess (strict), not CanView: same shape as RelatedProductList/BundleProductList above. Admin's
    // original CrossSellProductList had no check at all.

    [TestMethod]
    public async Task CrossSellProductList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);

        var result = await _controller.CrossSellProductList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
    }

    [TestMethod]
    public async Task CrossSellProductList_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        product.CrossSellProduct.Add("p2");
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _productServiceMock.Setup(p => p.GetProductById("p2", false)).ReturnsAsync(new Product { Id = "p2", Name = "Second" });
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);

        var result = await _controller.CrossSellProductList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- CrossSellProductDelete -------------------------------------------------------------------
    // Admin/Store both throw ArgumentException when the product does not exist; Vendor's original
    // CrossSellProductDelete had no ownership check at all - closed here the same way as List above.

    [TestMethod]
    public async Task CrossSellProductDelete_ProductNotFound_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync((Product)null);
        var model = new ProductModel.CrossSellProductModel { ProductId = "p1", Id = "p2" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.CrossSellProductDelete(model));
    }

    [TestMethod]
    public async Task CrossSellProductDelete_ScopeDeniesAccess_ReturnsErrorJson_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        product.CrossSellProduct.Add("p2");
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.CrossSellProductModel { ProductId = "p1", Id = "p2" };

        var result = await _controller.CrossSellProductDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productRelatedViewModelServiceMock.Verify(s => s.DeleteCrossSellProduct(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task CrossSellProductDelete_NoMatchingCrossSellProduct_Throws()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.CrossSellProductModel { ProductId = "p1", Id = "p2" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.CrossSellProductDelete(model));
    }

    [TestMethod]
    public async Task CrossSellProductDelete_ScopeGrantsAccess_ValidModel_Deletes()
    {
        var product = new Product { Id = "p1" };
        product.CrossSellProduct.Add("p2");
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.CrossSellProductModel { ProductId = "p1", Id = "p2" };

        var result = await _controller.CrossSellProductDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productRelatedViewModelServiceMock.Verify(s => s.DeleteCrossSellProduct("p1", "p2"), Times.Once);
    }

    // --- CrossSellProductAddPopup (GET) -------------------------------------------------------------

    [TestMethod]
    public async Task CrossSellProductAddPopupGet_CallsPrepareCrossSellProductModel()
    {
        // PrepareCrossSellProductModel() now reads scope internally (Task 9).
        _productAddPopupViewModelServiceMock.Setup(s => s.PrepareCrossSellProductModel())
            .ReturnsAsync(new ProductModel.AddCrossSellProductModel());

        var result = await _controller.CrossSellProductAddPopup("p1") as ViewResult;

        Assert.IsNotNull(result);
        var model = result.Model as ProductModel.AddCrossSellProductModel;
        Assert.IsNotNull(model);
        Assert.AreEqual("p1", model.ProductId);
        _productAddPopupViewModelServiceMock.Verify(s => s.PrepareCrossSellProductModel(), Times.Once);
    }

    // --- CrossSellProductAddPopupList ---------------------------------------------------------------

    [TestMethod]
    public async Task CrossSellProductAddPopupList_UsesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _productAddPopupViewModelServiceMock
            .Setup(s => s.PrepareProductModel(It.IsAny<ProductModel.AddCrossSellProductModel>(), 0, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var model = new ProductModel.AddCrossSellProductModel();
        var result = await _controller.CrossSellProductAddPopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 0, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("store-1", model.SearchStoreId);
    }

    [TestMethod]
    public async Task CrossSellProductAddPopupList_NoDefaultStoreId_DoesNotOverrideModelSearchStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _productAddPopupViewModelServiceMock
            .Setup(s => s.PrepareProductModel(It.IsAny<ProductModel.AddCrossSellProductModel>(), 0, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var model = new ProductModel.AddCrossSellProductModel { SearchStoreId = "explicit" };
        var result = await _controller.CrossSellProductAddPopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 0, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("explicit", model.SearchStoreId);
    }

    // --- CrossSellProductAddPopup (POST) --------------------------------------------------------------
    // HasAccess (strict): closes a real gap - Vendor's original CrossSellProductAddPopup(POST) had no
    // ownership check at all, letting any vendor attach cross-sell-product mappings onto another
    // vendor's product by posting its id.

    [TestMethod]
    public async Task CrossSellProductAddPopupPost_ScopeDeniesAccess_ReturnsContentMessage_DoesNotInsert()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.AddCrossSellProductModel { ProductId = "p1", SelectedProductIds = ["p2"] };

        var result = await _controller.CrossSellProductAddPopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productRelatedViewModelServiceMock.Verify(s => s.InsertCrossSellProductModel(It.IsAny<ProductModel.AddCrossSellProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task CrossSellProductAddPopupPost_ScopeGrantsAccess_ValidModel_Inserts()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.AddCrossSellProductModel { ProductId = "p1", SelectedProductIds = ["p2"] };

        var result = await _controller.CrossSellProductAddPopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("", content.Content);
        _productRelatedViewModelServiceMock.Verify(s => s.InsertCrossSellProductModel(model), Times.Once);
    }

    [TestMethod]
    public async Task CrossSellProductAddPopupPost_ScopeGrantsAccess_InvalidModel_ReturnsViewWithRepreparedModel()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var reprepared = new ProductModel.AddCrossSellProductModel();
        _productAddPopupViewModelServiceMock.Setup(s => s.PrepareCrossSellProductModel()).ReturnsAsync(reprepared);
        var model = new ProductModel.AddCrossSellProductModel { ProductId = "p1" };
        _controller.ModelState.AddModelError("x", "error");

        var result = await _controller.CrossSellProductAddPopup(model);

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreSame(reprepared, view.Model);
        _productRelatedViewModelServiceMock.Verify(s => s.InsertCrossSellProductModel(It.IsAny<ProductModel.AddCrossSellProductModel>()), Times.Never);
    }

    // --- RecommendedProductList ---------------------------------------------------------------------
    // HasAccess (strict), not CanView: same shape as CrossSellProductList above. Admin's original
    // RecommendedProductList had no check at all. Vendor's original signature also dropped the
    // DataSourceRequest command parameter entirely - kept here for parity with Admin/Store.

    [TestMethod]
    public async Task RecommendedProductList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);

        var result = await _controller.RecommendedProductList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
    }

    [TestMethod]
    public async Task RecommendedProductList_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        product.RecommendedProduct.Add("p2");
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _productServiceMock.Setup(p => p.GetProductById("p2", false)).ReturnsAsync(new Product { Id = "p2", Name = "Second" });
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);

        var result = await _controller.RecommendedProductList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- RecommendedProductDelete -------------------------------------------------------------------
    // Admin/Store/Vendor all throw ArgumentException when the product does not exist; Vendor's original
    // RecommendedProductDelete had no ownership check at all - closed here the same way as List above.

    [TestMethod]
    public async Task RecommendedProductDelete_ProductNotFound_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync((Product)null);
        var model = new ProductModel.RecommendedProductModel { ProductId = "p1", Id = "p2" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.RecommendedProductDelete(model));
    }

    [TestMethod]
    public async Task RecommendedProductDelete_ScopeDeniesAccess_ReturnsErrorJson_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        product.RecommendedProduct.Add("p2");
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.RecommendedProductModel { ProductId = "p1", Id = "p2" };

        var result = await _controller.RecommendedProductDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productRelatedViewModelServiceMock.Verify(s => s.DeleteRecommendedProduct(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task RecommendedProductDelete_NoMatchingRecommendedProduct_Throws()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.RecommendedProductModel { ProductId = "p1", Id = "p2" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.RecommendedProductDelete(model));
    }

    [TestMethod]
    public async Task RecommendedProductDelete_ScopeGrantsAccess_ValidModel_Deletes()
    {
        var product = new Product { Id = "p1" };
        product.RecommendedProduct.Add("p2");
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.RecommendedProductModel { ProductId = "p1", Id = "p2" };

        var result = await _controller.RecommendedProductDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productRelatedViewModelServiceMock.Verify(s => s.DeleteRecommendedProduct("p1", "p2"), Times.Once);
    }

    // --- RecommendedProductAddPopup (GET) -------------------------------------------------------------

    [TestMethod]
    public async Task RecommendedProductAddPopupGet_CallsPrepareRecommendedProductModel()
    {
        // PrepareRecommendedProductModel() now reads scope internally (Task 9).
        _productAddPopupViewModelServiceMock.Setup(s => s.PrepareRecommendedProductModel())
            .ReturnsAsync(new ProductModel.AddRecommendedProductModel());

        var result = await _controller.RecommendedProductAddPopup("p1") as ViewResult;

        Assert.IsNotNull(result);
        var model = result.Model as ProductModel.AddRecommendedProductModel;
        Assert.IsNotNull(model);
        Assert.AreEqual("p1", model.ProductId);
        _productAddPopupViewModelServiceMock.Verify(s => s.PrepareRecommendedProductModel(), Times.Once);
    }

    // --- RecommendedProductAddPopupList ---------------------------------------------------------------

    [TestMethod]
    public async Task RecommendedProductAddPopupList_UsesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _productAddPopupViewModelServiceMock
            .Setup(s => s.PrepareProductModel(It.IsAny<ProductModel.AddRecommendedProductModel>(), 0, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var model = new ProductModel.AddRecommendedProductModel();
        var result = await _controller.RecommendedProductAddPopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 0, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("store-1", model.SearchStoreId);
    }

    [TestMethod]
    public async Task RecommendedProductAddPopupList_NoDefaultStoreId_DoesNotOverrideModelSearchStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _productAddPopupViewModelServiceMock
            .Setup(s => s.PrepareProductModel(It.IsAny<ProductModel.AddRecommendedProductModel>(), 0, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var model = new ProductModel.AddRecommendedProductModel { SearchStoreId = "explicit" };
        var result = await _controller.RecommendedProductAddPopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 0, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("explicit", model.SearchStoreId);
    }

    // --- RecommendedProductAddPopup (POST) --------------------------------------------------------------
    // HasAccess (strict): closes a real gap - Vendor's original RecommendedProductAddPopup(POST) had no
    // ownership check at all, letting any vendor attach recommended-product mappings onto another
    // vendor's product by posting its id.

    [TestMethod]
    public async Task RecommendedProductAddPopupPost_ScopeDeniesAccess_ReturnsContentMessage_DoesNotInsert()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.AddRecommendedProductModel { ProductId = "p1", SelectedProductIds = ["p2"] };

        var result = await _controller.RecommendedProductAddPopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productRelatedViewModelServiceMock.Verify(s => s.InsertRecommendedProductModel(It.IsAny<ProductModel.AddRecommendedProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task RecommendedProductAddPopupPost_ScopeGrantsAccess_ValidModel_Inserts()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.AddRecommendedProductModel { ProductId = "p1", SelectedProductIds = ["p2"] };

        var result = await _controller.RecommendedProductAddPopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("", content.Content);
        _productRelatedViewModelServiceMock.Verify(s => s.InsertRecommendedProductModel(model), Times.Once);
    }

    [TestMethod]
    public async Task RecommendedProductAddPopupPost_ScopeGrantsAccess_InvalidModel_ReturnsViewWithRepreparedModel()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var reprepared = new ProductModel.AddRecommendedProductModel();
        _productAddPopupViewModelServiceMock.Setup(s => s.PrepareRecommendedProductModel()).ReturnsAsync(reprepared);
        var model = new ProductModel.AddRecommendedProductModel { ProductId = "p1" };
        _controller.ModelState.AddModelError("x", "error");

        var result = await _controller.RecommendedProductAddPopup(model);

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreSame(reprepared, view.Model);
        _productRelatedViewModelServiceMock.Verify(s => s.InsertRecommendedProductModel(It.IsAny<ProductModel.AddRecommendedProductModel>()), Times.Never);
    }
}
