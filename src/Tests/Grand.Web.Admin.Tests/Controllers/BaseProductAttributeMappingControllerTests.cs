using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
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
public class BaseProductAttributeMappingControllerTests : ProductControllerTestsBase<BaseProductAttributeMappingControllerTests.TestProductAttributeMappingController>
{
    // BaseProductAttributeMappingController is abstract. This minimal subclass exists only so the actions under test
    // can be invoked directly.
    public class TestProductAttributeMappingController(
        IProductViewModelService productViewModelService,
        IProductService productService,
        ILanguageService languageService,
        IRequestTranslationService translationService,
        IAdminDataScope<Product> scope)
        : BaseProductAttributeMappingController(productViewModelService, productService, languageService, translationService, scope);

    protected override TestProductAttributeMappingController CreateController() => new(
        _productViewModelServiceMock.Object,
        _productServiceMock.Object,
        _languageServiceMock.Object,
        _translationServiceMock.Object,
        _scopeMock.Object);

    // --- ProductAttributeMappingList -------------------------------------------------------------
    // HasAccess applied uniformly on every action in this region. Store's original checked ownership
    // (CanAccessProduct) on List/PopupGET/PopupPOST/Delete/ValidationRulesPopupGET but not on
    // ValidationRulesPopup(POST). Vendor's original checked (CheckAccessToProduct/HasAccessToProduct) on
    // List/PopupGET/Delete/ValidationRulesPopupGET, but NOT on ProductAttributeMappingPopup(POST) or
    // ValidationRulesPopup(POST) - letting a vendor edit an attribute mapping (or its validation rules) on
    // any product, not just their own, by posting a known productId.

    [TestMethod]
    public async Task ProductAttributeMappingList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);

        var result = await _controller.ProductAttributeMappingList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductAttributeMappingModels(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeMappingList_ScopeDeniesAccess_UsesScopeResourceKeyPrefix()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);
        _scopeMock.Setup(s => s.ResourceKeyPrefix).Returns("Vendor");

        var result = await _controller.ProductAttributeMappingList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Vendor.Catalog.Products.Permissions"), Times.Once);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeMappingList_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);
        var attributes = new List<ProductModel.ProductAttributeMappingModel> { new() { Id = "pam1" } };
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeMappingModels(product))
            .ReturnsAsync(attributes);

        var result = await _controller.ProductAttributeMappingList(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- ProductAttributeMappingPopup (GET) ------------------------------------------------------

    [TestMethod]
    public async Task ProductAttributeMappingPopup_Get_ScopeDeniesAccess_ReturnsPermissionsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductAttributeMappingPopup("p1", null);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductAttributeMappingModel(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeMappingPopup_Get_ScopeGrantsAccess_NoMappingId_PreparesNewModel()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeMappingModel(product))
            .ReturnsAsync(new ProductModel.ProductAttributeMappingModel());

        var result = await _controller.ProductAttributeMappingPopup("p1", null);

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(s => s.PrepareProductAttributeMappingModel(product), Times.Once);
    }

    [TestMethod]
    public async Task ProductAttributeMappingPopup_Get_ScopeGrantsAccess_MappingId_PreparesEditModel()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeMappingModel(product, mapping))
            .ReturnsAsync(new ProductModel.ProductAttributeMappingModel());

        var result = await _controller.ProductAttributeMappingPopup("p1", "pam1");

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(s => s.PrepareProductAttributeMappingModel(product, mapping), Times.Once);
    }

    // --- ProductAttributeMappingPopup (POST) -----------------------------------------------------
    // HasAccess added here: Vendor's original had no ownership check on this POST at all.

    [TestMethod]
    public async Task ProductAttributeMappingPopup_Post_ScopeDeniesAccess_DoesNotInsertOrUpdate()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductAttributeMappingModel { ProductId = "p1" };

        var result = await _controller.ProductAttributeMappingPopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.InsertProductAttributeMappingModel(It.IsAny<ProductModel.ProductAttributeMappingModel>()),
            Times.Never);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductAttributeMappingModel(It.IsAny<ProductModel.ProductAttributeMappingModel>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeMappingPopup_Post_ScopeGrantsAccess_NoId_Inserts()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductAttributeMappingModel { ProductId = "p1" };

        var result = await _controller.ProductAttributeMappingPopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _productViewModelServiceMock.Verify(s => s.InsertProductAttributeMappingModel(model), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductAttributeMappingModel(It.IsAny<ProductModel.ProductAttributeMappingModel>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeMappingPopup_Post_ScopeGrantsAccess_WithId_Updates()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductAttributeMappingModel { Id = "pam1", ProductId = "p1" };

        var result = await _controller.ProductAttributeMappingPopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _productViewModelServiceMock.Verify(s => s.UpdateProductAttributeMappingModel(model), Times.Once);
    }

    [TestMethod]
    public async Task ProductAttributeMappingPopup_Post_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);
        var model = new ProductModel.ProductAttributeMappingModel { ProductId = "missing" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.ProductAttributeMappingPopup(model));
    }

    [TestMethod]
    public async Task ProductAttributeMappingPopup_Post_InvalidModelState_ReturnsView_DoesNotCheckAccess()
    {
        _controller.ModelState.AddModelError("x", "err");
        var model = new ProductModel.ProductAttributeMappingModel { ProductId = "p1" };
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeMappingModel(model)).ReturnsAsync(model);

        var result = await _controller.ProductAttributeMappingPopup(model);

        Assert.IsInstanceOfType<ViewResult>(result);
        _productServiceMock.Verify(p => p.GetProductById(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        _scopeMock.Verify(s => s.HasAccess(It.IsAny<Product>()), Times.Never);
    }

    // --- ProductAttributeMappingDelete -----------------------------------------------------------

    [TestMethod]
    public async Task ProductAttributeMappingDelete_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);
        var attrServiceMock = new Mock<IProductAttributeService>();

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeMappingDelete("pam1", "missing", attrServiceMock.Object));
    }

    [TestMethod]
    public async Task ProductAttributeMappingDelete_MappingNotFound_Throws()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        var attrServiceMock = new Mock<IProductAttributeService>();

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeMappingDelete("missing-pam", "p1", attrServiceMock.Object));
    }

    [TestMethod]
    public async Task ProductAttributeMappingDelete_ScopeDeniesAccess_ReturnsErrorJson_DoesNotDelete()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var attrServiceMock = new Mock<IProductAttributeService>();

        var result = await _controller.ProductAttributeMappingDelete("pam1", "p1", attrServiceMock.Object);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        attrServiceMock.Verify(
            s => s.DeleteProductAttributeMapping(It.IsAny<ProductAttributeMapping>(), It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeMappingDelete_ScopeGrantsAccess_Deletes()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var attrServiceMock = new Mock<IProductAttributeService>();

        var result = await _controller.ProductAttributeMappingDelete("pam1", "p1", attrServiceMock.Object);

        Assert.IsInstanceOfType<JsonResult>(result);
        attrServiceMock.Verify(s => s.DeleteProductAttributeMapping(mapping, "p1"), Times.Once);
    }

    // --- ProductAttributeValidationRulesPopup (GET) -----------------------------------------------

    [TestMethod]
    public async Task ProductAttributeValidationRulesPopup_Get_ScopeDeniesAccess_ReturnsPermissionsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductAttributeValidationRulesPopup("pam1", "p1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
    }

    [TestMethod]
    public async Task ProductAttributeValidationRulesPopup_Get_ScopeGrantsAccess_MappingMissing_ReturnsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.ProductAttributeValidationRulesPopup("missing-pam", "p1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("No attribute value found with the specified id", content.Content);
    }

    [TestMethod]
    public async Task ProductAttributeValidationRulesPopup_Get_ScopeGrantsAccess_PreparesModel()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeMappingModel(mapping))
            .ReturnsAsync(new ProductModel.ProductAttributeMappingModel());

        var result = await _controller.ProductAttributeValidationRulesPopup("pam1", "p1");

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(s => s.PrepareProductAttributeMappingModel(mapping), Times.Once);
    }

    // --- ProductAttributeValidationRulesPopup (POST) ----------------------------------------------
    // HasAccess added here: none of the three original hosts checked ownership on this POST at all - a
    // store/vendor user could update an attribute mapping's validation rules on any product by posting a
    // known productId/model.Id.

    [TestMethod]
    public async Task ProductAttributeValidationRulesPopup_Post_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);
        var model = new ProductModel.ProductAttributeMappingModel { ProductId = "missing" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeValidationRulesPopup(model));
    }

    [TestMethod]
    public async Task ProductAttributeValidationRulesPopup_Post_MappingNotFound_Throws()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        var model = new ProductModel.ProductAttributeMappingModel { Id = "missing-pam", ProductId = "p1" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeValidationRulesPopup(model));
    }

    [TestMethod]
    public async Task ProductAttributeValidationRulesPopup_Post_ScopeDeniesAccess_ReturnsPermissionsContent_DoesNotUpdate()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductAttributeMappingModel { Id = "pam1", ProductId = "p1" };

        var result = await _controller.ProductAttributeValidationRulesPopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductAttributeValidationRulesModel(It.IsAny<ProductAttributeMapping>(),
                It.IsAny<ProductModel.ProductAttributeMappingModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeValidationRulesPopup_Post_ScopeGrantsAccess_ValidModel_Updates()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductAttributeMappingModel { Id = "pam1", ProductId = "p1" };

        var result = await _controller.ProductAttributeValidationRulesPopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductAttributeValidationRulesModel(mapping, model), Times.Once);
    }

    [TestMethod]
    public async Task ProductAttributeValidationRulesPopup_Post_ScopeGrantsAccess_InvalidModelState_ReturnsView()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _controller.ModelState.AddModelError("x", "err");
        var model = new ProductModel.ProductAttributeMappingModel { Id = "pam1", ProductId = "p1" };
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeMappingModel(mapping)).ReturnsAsync(model);

        var result = await _controller.ProductAttributeValidationRulesPopup(model);

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductAttributeValidationRulesModel(It.IsAny<ProductAttributeMapping>(),
                It.IsAny<ProductModel.ProductAttributeMappingModel>()), Times.Never);
    }

    // --- ProductAttributeConditionPopup (GET) ----------------------------------------------------
    // HasAccess added uniformly. Admin's original had no ownership check on either action of this
    // region at all.

    [TestMethod]
    public async Task ProductAttributeConditionPopup_Get_ScopeDeniesAccess_ReturnsPermissionsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductAttributeConditionPopup("p1", "pam1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductAttributeConditionModel(It.IsAny<Product>(), It.IsAny<ProductAttributeMapping>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeConditionPopup_Get_ScopeDeniesAccess_UsesScopeResourceKeyPrefix()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        _scopeMock.Setup(s => s.ResourceKeyPrefix).Returns("Vendor");

        var result = await _controller.ProductAttributeConditionPopup("p1", "pam1");

        Assert.IsInstanceOfType<ContentResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Vendor.Catalog.Products.Permissions"), Times.Once);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeConditionPopup_Get_ScopeGrantsAccess_MappingMissing_ReturnsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.ProductAttributeConditionPopup("p1", "missing-pam");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("No attribute value found with the specified id", content.Content);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductAttributeConditionModel(It.IsAny<Product>(), It.IsAny<ProductAttributeMapping>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeConditionPopup_Get_ScopeGrantsAccess_PreparesModel()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeConditionModel(product, mapping))
            .ReturnsAsync(new ProductAttributeConditionModel());

        var result = await _controller.ProductAttributeConditionPopup("p1", "pam1");

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(s => s.PrepareProductAttributeConditionModel(product, mapping),
            Times.Once);
    }

    // --- ProductAttributeConditionPopup (POST) ---------------------------------------------------
    // HasAccess added here: Vendor's original never checked ownership on this POST at all (only its
    // GET sibling did, via CheckAccessToProduct), letting a vendor update an attribute condition on
    // any product by posting a known productId/productAttributeMappingId. Admin's original had no
    // check on either action.

    [TestMethod]
    public async Task ProductAttributeConditionPopup_Post_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);
        var model = new ProductAttributeConditionModel { ProductId = "missing" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.ProductAttributeConditionPopup(model));
    }

    [TestMethod]
    public async Task ProductAttributeConditionPopup_Post_MappingNotFound_ReturnsContent_DoesNotCheckAccess()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        var model = new ProductAttributeConditionModel { ProductId = "p1", ProductAttributeMappingId = "missing-pam" };

        var result = await _controller.ProductAttributeConditionPopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("No attribute value found with the specified id", content.Content);
        _scopeMock.Verify(s => s.HasAccess(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeConditionPopup_Post_ScopeDeniesAccess_ReturnsPermissionsContent_DoesNotUpdate()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductAttributeConditionModel { ProductId = "p1", ProductAttributeMappingId = "pam1" };

        var result = await _controller.ProductAttributeConditionPopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductAttributeConditionModel(It.IsAny<Product>(), It.IsAny<ProductAttributeMapping>(),
                It.IsAny<ProductAttributeConditionModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeConditionPopup_Post_ScopeGrantsAccess_Updates()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductAttributeConditionModel { ProductId = "p1", ProductAttributeMappingId = "pam1" };

        var result = await _controller.ProductAttributeConditionPopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _productViewModelServiceMock.Verify(s => s.UpdateProductAttributeConditionModel(product, mapping, model),
            Times.Once);
    }

    // --- EditAttributeValues (GET) ------------------------------------------------------------------
    // HasAccess applied uniformly. Admin's original had no ownership check on this action at all.

    [TestMethod]
    public async Task EditAttributeValues_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);
        var attrServiceMock = new Mock<IProductAttributeService>();

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.EditAttributeValues("pam1", "missing", attrServiceMock.Object));
    }

    [TestMethod]
    public async Task EditAttributeValues_ScopeDeniesAccess_ReturnsPermissionsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var attrServiceMock = new Mock<IProductAttributeService>();

        var result = await _controller.EditAttributeValues("pam1", "p1", attrServiceMock.Object);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
    }

    [TestMethod]
    public async Task EditAttributeValues_ScopeGrantsAccess_MappingMissing_Throws()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var attrServiceMock = new Mock<IProductAttributeService>();

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.EditAttributeValues("missing-pam", "p1", attrServiceMock.Object));
    }

    [TestMethod]
    public async Task EditAttributeValues_ScopeGrantsAccess_PreparesListModel()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1", ProductAttributeId = "pa1" };
        var product = new Product { Id = "p1", Name = "Product 1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var attrServiceMock = new Mock<IProductAttributeService>();
        attrServiceMock.Setup(s => s.GetProductAttributeById("pa1"))
            .ReturnsAsync(new ProductAttribute { Id = "pa1", Name = "Color" });

        var result = await _controller.EditAttributeValues("pam1", "p1", attrServiceMock.Object);

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        var model = view.Model as ProductModel.ProductAttributeValueListModel;
        Assert.IsNotNull(model);
        Assert.AreEqual("p1", model.ProductId);
        Assert.AreEqual("Product 1", model.ProductName);
        Assert.AreEqual("Color", model.ProductAttributeName);
        Assert.AreEqual("pam1", model.ProductAttributeMappingId);
    }

    // --- ProductAttributeValueList (POST) -----------------------------------------------------------

    [TestMethod]
    public async Task ProductAttributeValueList_ScopeDeniesAccess_ReturnsErrorJson()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductAttributeValueList("pam1", "p1", new Grand.Web.Common.DataSource.DataSourceRequest());

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductAttributeValueModels(It.IsAny<Product>(), It.IsAny<ProductAttributeMapping>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeValueList_ScopeGrantsAccess_MappingMissing_Throws()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeValueList("missing-pam", "p1", new Grand.Web.Common.DataSource.DataSourceRequest()));
    }

    [TestMethod]
    public async Task ProductAttributeValueList_ScopeGrantsAccess_ReturnsGrid()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var values = new List<ProductModel.ProductAttributeValueModel> { new() { Id = "pav1" } };
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeValueModels(product, mapping))
            .ReturnsAsync(values);

        var result = await _controller.ProductAttributeValueList("pam1", "p1", new Grand.Web.Common.DataSource.DataSourceRequest());

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- ProductAttributeValueCreatePopup (GET) -----------------------------------------------------

    [TestMethod]
    public async Task ProductAttributeValueCreatePopup_Get_ScopeDeniesAccess_ReturnsPermissionsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductAttributeValueCreatePopup("pam1", "p1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.PrepareProductAttributeValueModel(It.IsAny<Product>(), It.IsAny<ProductAttributeMapping>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeValueCreatePopup_Get_ScopeGrantsAccess_MappingMissing_Throws()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeValueCreatePopup("missing-pam", "p1"));
    }

    [TestMethod]
    public async Task ProductAttributeValueCreatePopup_Get_ScopeGrantsAccess_PreparesModel()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeValueModel(product, mapping))
            .ReturnsAsync(new ProductModel.ProductAttributeValueModel());

        var result = await _controller.ProductAttributeValueCreatePopup("pam1", "p1");

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(s => s.PrepareProductAttributeValueModel(product, mapping), Times.Once);
    }

    // --- ProductAttributeValueCreatePopup (POST) ----------------------------------------------------
    // HasAccess added explicitly. Admin has no ownership concept at all (GlobalAdminDataScope.HasAccess is
    // always true), so Admin was never at risk here. The real risk is to VENDOR: Vendor's original relied
    // solely on ProductAttributeValueModel : IProductValidVendor triggering ValidationFilter's
    // ProductValidVendor check + Vendor's own `if (ModelState.IsValid)` gate - no explicit action-level
    // check. Moving to the shared AdminShared model (no IProductValidVendor) would have silently dropped
    // that guard for Vendor; scope.HasAccess replaces it explicitly and uniformly.

    [TestMethod]
    public async Task ProductAttributeValueCreatePopup_Post_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);
        var model = new ProductModel.ProductAttributeValueModel { ProductId = "missing" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeValueCreatePopup(model));
    }

    [TestMethod]
    public async Task ProductAttributeValueCreatePopup_Post_ScopeDeniesAccess_ReturnsPermissionsContent_DoesNotInsert()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductAttributeValueModel { ProductId = "p1" };

        var result = await _controller.ProductAttributeValueCreatePopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.InsertProductAttributeValueModel(It.IsAny<ProductModel.ProductAttributeValueModel>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeValueCreatePopup_Post_ScopeDeniesAccess_UsesVendorResourceKeyPrefix()
    {
        // The host/action combination this row's fix actually protects: Vendor losing its
        // IProductValidVendor-driven guard in the merge. See the comment above the HasAccess check in
        // BaseProductController.ProductAttributeValueCreatePopup(POST).
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        _scopeMock.Setup(s => s.ResourceKeyPrefix).Returns("Vendor");
        var model = new ProductModel.ProductAttributeValueModel { ProductId = "p1" };

        var result = await _controller.ProductAttributeValueCreatePopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Vendor.Catalog.Products.Permissions"), Times.Once);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Never);
        _productViewModelServiceMock.Verify(
            s => s.InsertProductAttributeValueModel(It.IsAny<ProductModel.ProductAttributeValueModel>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeValueCreatePopup_Post_ScopeGrantsAccess_MappingMissing_RedirectsToList()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductAttributeValueModel { ProductId = "p1", ProductAttributeMappingId = "missing-pam" };

        var result = await _controller.ProductAttributeValueCreatePopup(model);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        Assert.AreEqual("Product", redirect.ControllerName);
    }

    [TestMethod]
    public async Task ProductAttributeValueCreatePopup_Post_ScopeGrantsAccess_ValidModel_Inserts()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductAttributeValueModel { ProductId = "p1", ProductAttributeMappingId = "pam1" };

        var result = await _controller.ProductAttributeValueCreatePopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _productViewModelServiceMock.Verify(s => s.InsertProductAttributeValueModel(model), Times.Once);
    }

    [TestMethod]
    public async Task ProductAttributeValueCreatePopup_Post_ScopeGrantsAccess_InvalidModelState_ReturnsView()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _controller.ModelState.AddModelError("x", "err");
        var model = new ProductModel.ProductAttributeValueModel { ProductId = "p1", ProductAttributeMappingId = "pam1" };

        var result = await _controller.ProductAttributeValueCreatePopup(model);

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.InsertProductAttributeValueModel(It.IsAny<ProductModel.ProductAttributeValueModel>()),
            Times.Never);
        _productViewModelServiceMock.Verify(s => s.PrepareProductAttributeValueModel(product, model), Times.Once);
    }

    // --- ProductAttributeValueEditPopup (GET) -------------------------------------------------------

    [TestMethod]
    public async Task ProductAttributeValueEditPopup_Get_ScopeDeniesAccess_ReturnsPermissionsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ProductAttributeValueEditPopup("pav1", "p1", "pam1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
    }

    [TestMethod]
    public async Task ProductAttributeValueEditPopup_Get_ScopeGrantsAccess_MappingMissing_RedirectsToList()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.ProductAttributeValueEditPopup("pav1", "p1", "missing-pam");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task ProductAttributeValueEditPopup_Get_ScopeGrantsAccess_ValueMissing_RedirectsToList()
    {
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);

        var result = await _controller.ProductAttributeValueEditPopup("missing-pav", "p1", "pam1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task ProductAttributeValueEditPopup_Get_ScopeGrantsAccess_PreparesModel()
    {
        var pav = new ProductAttributeValue { Id = "pav1" };
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        mapping.ProductAttributeValues.Add(pav);
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductAttributeValueModel();
        _productViewModelServiceMock.Setup(s => s.PrepareProductAttributeValueModel(mapping, pav))
            .ReturnsAsync(model);

        var result = await _controller.ProductAttributeValueEditPopup("pav1", "p1", "pam1");

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(s => s.PrepareProductAttributeValueModel(mapping, pav), Times.Once);
        _productViewModelServiceMock.Verify(s => s.PrepareProductAttributeValueModel(product, model), Times.Once);
    }

    // --- ProductAttributeValueEditPopup (POST) ------------------------------------------------------
    // HasAccess added here: Vendor's original never checked ownership on this POST at all (only its GET
    // sibling did); Admin's original had no check on either action.

    [TestMethod]
    public async Task ProductAttributeValueEditPopup_Post_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);
        var model = new ProductModel.ProductAttributeValueModel();

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeValueEditPopup("missing", model));
    }

    [TestMethod]
    public async Task ProductAttributeValueEditPopup_Post_ScopeDeniesAccess_ReturnsPermissionsContent_DoesNotUpdate()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductAttributeValueModel { Id = "pav1", ProductAttributeMappingId = "pam1" };

        var result = await _controller.ProductAttributeValueEditPopup("p1", model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductAttributeValueModel(It.IsAny<ProductAttributeValue>(),
                It.IsAny<ProductModel.ProductAttributeValueModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeValueEditPopup_Post_ScopeGrantsAccess_ValueMissing_RedirectsToList()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductAttributeValueModel { Id = "missing-pav", ProductAttributeMappingId = "pam1" };

        var result = await _controller.ProductAttributeValueEditPopup("p1", model);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task ProductAttributeValueEditPopup_Post_ScopeGrantsAccess_ValidModel_Updates()
    {
        var pav = new ProductAttributeValue { Id = "pav1" };
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        mapping.ProductAttributeValues.Add(pav);
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductAttributeValueModel { Id = "pav1", ProductAttributeMappingId = "pam1" };

        var result = await _controller.ProductAttributeValueEditPopup("p1", model);

        Assert.IsInstanceOfType<ContentResult>(result);
        _productViewModelServiceMock.Verify(s => s.UpdateProductAttributeValueModel(pav, model), Times.Once);
    }

    [TestMethod]
    public async Task ProductAttributeValueEditPopup_Post_ScopeGrantsAccess_InvalidModelState_ReturnsView()
    {
        var pav = new ProductAttributeValue { Id = "pav1" };
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        mapping.ProductAttributeValues.Add(pav);
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _controller.ModelState.AddModelError("x", "err");
        var model = new ProductModel.ProductAttributeValueModel { Id = "pav1", ProductAttributeMappingId = "pam1" };

        var result = await _controller.ProductAttributeValueEditPopup("p1", model);

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(
            s => s.UpdateProductAttributeValueModel(It.IsAny<ProductAttributeValue>(),
                It.IsAny<ProductModel.ProductAttributeValueModel>()), Times.Never);
        _productViewModelServiceMock.Verify(s => s.PrepareProductAttributeValueModel(product, model), Times.Once);
    }

    // --- ProductAttributeValueDelete -----------------------------------------------------------------
    // HasAccess added here: this action takes only simple string parameters, so no model-level validator
    // ever runs for it. Admin's original had no ownership check at all.

    [TestMethod]
    public async Task ProductAttributeValueDelete_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);
        var attrServiceMock = new Mock<IProductAttributeService>();

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeValueDelete("pav1", "pam1", "missing", attrServiceMock.Object));
    }

    [TestMethod]
    public async Task ProductAttributeValueDelete_ScopeDeniesAccess_ReturnsErrorJson_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var attrServiceMock = new Mock<IProductAttributeService>();

        var result = await _controller.ProductAttributeValueDelete("pav1", "pam1", "p1", attrServiceMock.Object);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        attrServiceMock.Verify(
            s => s.DeleteProductAttributeValue(It.IsAny<ProductAttributeValue>(), It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAttributeValueDelete_ScopeGrantsAccess_ValueMissing_Throws()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var attrServiceMock = new Mock<IProductAttributeService>();

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ProductAttributeValueDelete("missing-pav", "pam1", "p1", attrServiceMock.Object));
    }

    [TestMethod]
    public async Task ProductAttributeValueDelete_ScopeGrantsAccess_ValidModel_Deletes()
    {
        var pav = new ProductAttributeValue { Id = "pav1" };
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        mapping.ProductAttributeValues.Add(pav);
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var attrServiceMock = new Mock<IProductAttributeService>();

        var result = await _controller.ProductAttributeValueDelete("pav1", "pam1", "p1", attrServiceMock.Object);

        Assert.IsInstanceOfType<JsonResult>(result);
        attrServiceMock.Verify(s => s.DeleteProductAttributeValue(pav, "p1", "pam1"), Times.Once);
    }

    [TestMethod]
    public async Task ProductAttributeValueDelete_ScopeGrantsAccess_InvalidModelState_ReturnsErrorJson_DoesNotDelete()
    {
        var pav = new ProductAttributeValue { Id = "pav1" };
        var mapping = new ProductAttributeMapping { Id = "pam1" };
        mapping.ProductAttributeValues.Add(pav);
        var product = new Product { Id = "p1" };
        product.ProductAttributeMappings.Add(mapping);
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _controller.ModelState.AddModelError("x", "err");
        var attrServiceMock = new Mock<IProductAttributeService>();

        var result = await _controller.ProductAttributeValueDelete("pav1", "pam1", "p1", attrServiceMock.Object);

        Assert.IsInstanceOfType<JsonResult>(result);
        attrServiceMock.Verify(
            s => s.DeleteProductAttributeValue(It.IsAny<ProductAttributeValue>(), It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
    }

    // --- AssociateProductToAttributeValuePopup (GET) ------------------------------------------------

    [TestMethod]
    public async Task AssociateProductToAttributeValuePopup_Get_CallsPrepareAssociateProductToAttributeValueModel()
    {
        // PrepareAssociateProductToAttributeValueModel() now reads scope internally (Task 9).
        _productViewModelServiceMock.Setup(s => s.PrepareAssociateProductToAttributeValueModel())
            .ReturnsAsync(new ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel());

        var result = await _controller.AssociateProductToAttributeValuePopup();

        Assert.IsInstanceOfType<ViewResult>(result);
        _productViewModelServiceMock.Verify(s => s.PrepareAssociateProductToAttributeValueModel(),
            Times.Once);
    }

    // --- AssociateProductToAttributeValuePopupList (POST) -------------------------------------------

    [TestMethod]
    public async Task AssociateProductToAttributeValuePopupList_DefaultStoreIdSet_AppliesSearchStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store1");
        var model = new ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel();
        _productViewModelServiceMock.Setup(s => s.PrepareProductModel(model, 1, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var result = await _controller.AssociateProductToAttributeValuePopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("store1", model.SearchStoreId);
    }

    [TestMethod]
    public async Task AssociateProductToAttributeValuePopupList_DefaultStoreIdNull_LeavesSearchStoreIdUntouched()
    {
        var model = new ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel
            { SearchStoreId = "preset" };
        _productViewModelServiceMock.Setup(s => s.PrepareProductModel(model, 1, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        var result = await _controller.AssociateProductToAttributeValuePopupList(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.IsInstanceOfType<JsonResult>(result);
        Assert.AreEqual("preset", model.SearchStoreId);
    }

    // --- AssociateProductToAttributeValuePopup (POST) -----------------------------------------------
    // HasAccess added here: Admin's original had no ownership check on the associated product at all.

    [TestMethod]
    public async Task AssociateProductToAttributeValuePopup_Post_AssociatedProductMissing_ReturnsContent()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing", false)).ReturnsAsync((Product)null);
        var model = new ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel
            { AssociatedToProductId = "missing" };

        var result = await _controller.AssociateProductToAttributeValuePopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("Cannot load a product", content.Content);
        _scopeMock.Verify(s => s.HasAccess(It.IsAny<Product>()), Times.Never);
    }

    [TestMethod]
    public async Task AssociateProductToAttributeValuePopup_Post_ScopeDeniesAccess_ReturnsPermissionsContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel
            { AssociatedToProductId = "p1" };

        var result = await _controller.AssociateProductToAttributeValuePopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
    }

    [TestMethod]
    public async Task AssociateProductToAttributeValuePopup_Post_ScopeGrantsAccess_ReturnsEmptyContent()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var model = new ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel
            { AssociatedToProductId = "p1" };

        var result = await _controller.AssociateProductToAttributeValuePopup(model);

        Assert.IsInstanceOfType<ContentResult>(result);
        var content = result as ContentResult;
        Assert.AreEqual("", content.Content);
    }
}
