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
public class BaseProductReservationControllerTests : ProductControllerTestsBase<BaseProductReservationControllerTests.TestProductReservationController>
{
    // BaseProductReservationController is abstract. This minimal subclass exists only so the actions under test
    // can be invoked directly.
    public class TestProductReservationController(
        IProductService productService,
        IRequestTranslationService translationService,
        IProductReservationService productReservationService,
        IEnumTranslationService enumTranslationService,
        IAdminDataScope<Product> scope)
        : BaseProductReservationController(productService, translationService, productReservationService, enumTranslationService, scope);

    protected override TestProductReservationController CreateController() => new(
        _productServiceMock.Object,
        _translationServiceMock.Object,
        _productReservationServiceMock.Object,
        _enumTranslationServiceMock.Object,
        _scopeMock.Object);

    private static ProductReservation NewReservation(string id, string productId, string orderId = "") =>
        new() { Id = id, ProductId = productId, OrderId = orderId, Date = DateTime.UtcNow };

    // --- ListReservations ----------------------------------------------------------------------------

    [TestMethod]
    public async Task ListReservations_ScopeDeniesAccess_ReturnsKendoGridError_DoesNotQuery()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);

        var result = await _controller.ListReservations(
            new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productReservationServiceMock.Verify(
            s => s.GetProductReservationsByProductId(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<DateTime?>(),
                It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public async Task ListReservations_ScopeGrantsAccess_ReturnsGrid()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var reservations = new PagedList<ProductReservation>(
            new List<ProductReservation> { NewReservation("r1", "p1") }, 0, 10);
        _productReservationServiceMock
            .Setup(s => s.GetProductReservationsByProductId("p1", null, null, 0, 10))
            .ReturnsAsync(reservations);

        var result = await _controller.ListReservations(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 1, PageSize = 10 }, "p1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(1, gridModel.Total);
    }

    // A product shared with other stores opens read-only on Store: the tab loads, but the
    // reservations (other stores' orders) are not listed.
    [TestMethod]
    public async Task ListReservations_ViewableButNotOwned_ReturnsEmptyGrid_DoesNotQuery()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ListReservations(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 1, PageSize = 10 }, "p1");

        var gridModel = (result as JsonResult)?.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(0, gridModel.Total);
        _translationServiceMock.Verify(t => t.GetResource(It.IsAny<string>()), Times.Never);
        _productReservationServiceMock.Verify(
            s => s.GetProductReservationsByProductId(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<DateTime?>(),
                It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    // --- GenerateCalendar ------------------------------------------------------------------------------

    private static ProductModel.GenerateCalendarModel ValidCalendarModel(IntervalUnit unit = IntervalUnit.Day) => new() {
        StartDate = new DateTime(2026, 1, 1),
        EndDate = new DateTime(2026, 1, 1),
        StartTime = new DateTime(2026, 1, 1, 8, 0, 0),
        EndTime = new DateTime(2026, 1, 1, 18, 0, 0),
        Interval = 1,
        IntervalUnit = (int)unit,
        Quantity = 1,
        Resource = "room1",
        // 2026-01-01 is a Thursday.
        Thursday = true
    };

    [TestMethod]
    public async Task GenerateCalendar_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing")).ReturnsAsync((Product)null);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.GenerateCalendar("missing", ValidCalendarModel()));
    }

    [TestMethod]
    public async Task GenerateCalendar_ScopeDeniesAccess_ReturnsErrorsJson_DoesNotUpdate()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.GenerateCalendar("p1", ValidCalendarModel());

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productReservationServiceMock.Verify(
            s => s.InsertProductReservation(It.IsAny<ProductReservation>()), Times.Never);
    }

    [TestMethod]
    public async Task GenerateCalendar_InvalidModelState_ReturnsErrorsJson_DoesNotInsert()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _productReservationServiceMock
            .Setup(s => s.GetProductReservationsByProductId("p1", null, null))
            .ReturnsAsync(new PagedList<ProductReservation>(new List<ProductReservation>(), 0, int.MaxValue));
        _controller.ModelState.AddModelError("Resource", "Required");

        var result = await _controller.GenerateCalendar("p1", ValidCalendarModel());

        Assert.IsInstanceOfType<JsonResult>(result);
        _productReservationServiceMock.Verify(
            s => s.InsertProductReservation(It.IsAny<ProductReservation>()), Times.Never);
    }

    [TestMethod]
    public async Task GenerateCalendar_ScopeGrantsAccess_Valid_InsertsReservationAndUpdatesProductFields()
    {
        var product = new Product { Id = "p1", IntervalUnitId = IntervalUnit.Day };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _productReservationServiceMock
            .Setup(s => s.GetProductReservationsByProductId("p1", null, null))
            .ReturnsAsync(new PagedList<ProductReservation>(new List<ProductReservation>(), 0, int.MaxValue));

        var result = await _controller.GenerateCalendar("p1", ValidCalendarModel());

        Assert.IsInstanceOfType<JsonResult>(result);
        _productReservationServiceMock.Verify(
            s => s.InsertProductReservation(It.Is<ProductReservation>(r =>
                r.ProductId == "p1" && r.Resource == "room1")), Times.Once);
        _productServiceMock.Verify(
            s => s.UpdateProductField(product, It.IsAny<Expression<Func<Product, int>>>(), 1), Times.Once);
    }

    // --- ClearCalendar ----------------------------------------------------------------------------------

    [TestMethod]
    public async Task ClearCalendar_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing")).ReturnsAsync((Product)null);

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.ClearCalendar("missing"));
    }

    [TestMethod]
    public async Task ClearCalendar_ScopeDeniesAccess_Throws_DoesNotDelete()
    {
        // Throws rather than returning Json({errors}): CreateOrUpdate.Calendar.cshtml's success
        // callback for this action never reads the response body (no else branch) on any of the three
        // hosts, so a 200 response would silently refresh the grid as if the clear succeeded.
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.ClearCalendar("p1"));

        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productReservationServiceMock.Verify(
            s => s.DeleteProductReservation(It.IsAny<ProductReservation>()), Times.Never);
    }

    [TestMethod]
    public async Task ClearCalendar_ScopeGrantsAccess_DeletesAllReservations()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var reservation = NewReservation("r1", "p1");
        _productReservationServiceMock
            .Setup(s => s.GetProductReservationsByProductId("p1", true, null))
            .ReturnsAsync(new PagedList<ProductReservation>(new List<ProductReservation> { reservation }, 0, int.MaxValue));

        var result = await _controller.ClearCalendar("p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _productReservationServiceMock.Verify(s => s.DeleteProductReservation(reservation), Times.Once);
    }

    // --- ClearOld ---------------------------------------------------------------------------------------

    [TestMethod]
    public async Task ClearOld_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing")).ReturnsAsync((Product)null);

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.ClearOld("missing"));
    }

    [TestMethod]
    public async Task ClearOld_ScopeDeniesAccess_Throws_DoesNotDelete()
    {
        // See the comment on ClearCalendar_ScopeDeniesAccess_Throws_DoesNotDelete above - same view-layer
        // gap applies to this action's response handling.
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.ClearOld("p1"));

        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productReservationServiceMock.Verify(
            s => s.DeleteProductReservation(It.IsAny<ProductReservation>()), Times.Never);
    }

    [TestMethod]
    public async Task ClearOld_ScopeGrantsAccess_DeletesOnlyPastReservations()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var oldReservation = new ProductReservation { Id = "old", ProductId = "p1", Date = DateTime.UtcNow.AddDays(-1) };
        var futureReservation = new ProductReservation { Id = "future", ProductId = "p1", Date = DateTime.UtcNow.AddDays(1) };
        _productReservationServiceMock
            .Setup(s => s.GetProductReservationsByProductId("p1", true, null))
            .ReturnsAsync(new PagedList<ProductReservation>(
                new List<ProductReservation> { oldReservation, futureReservation }, 0, int.MaxValue));

        var result = await _controller.ClearOld("p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _productReservationServiceMock.Verify(s => s.DeleteProductReservation(oldReservation), Times.Once);
        _productReservationServiceMock.Verify(s => s.DeleteProductReservation(futureReservation), Times.Never);
    }

    // --- ProductReservationDelete ------------------------------------------------------------------------

    [TestMethod]
    public async Task ProductReservationDelete_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing")).ReturnsAsync((Product)null);
        var model = new ProductModel.ReservationModel { ProductId = "missing", ReservationId = "r1" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.ProductReservationDelete(model));
    }

    [TestMethod]
    public async Task ProductReservationDelete_ScopeDeniesAccess_ReturnsKendoGridError_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.ReservationModel { ProductId = "p1", ReservationId = "r1" };

        var result = await _controller.ProductReservationDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productReservationServiceMock.Verify(
            s => s.DeleteProductReservation(It.IsAny<ProductReservation>()), Times.Never);
    }

    // Regression guard for a real cross-product IDOR found in Store's and Vendor's original code: the
    // access check ran against model.ProductId while the delete ran against model.ReservationId, with no
    // check that the two referred to the same product. A caller with access to *any* product could pass
    // that product's id for the access check while supplying the ReservationId of a reservation belonging
    // to a different, unowned product, deleting it.
    [TestMethod]
    public async Task ProductReservationDelete_ReservationBelongsToDifferentProduct_ReturnsKendoGridError_DoesNotDelete()
    {
        var ownedProduct = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(ownedProduct);
        _scopeMock.Setup(s => s.HasAccess(ownedProduct)).ReturnsAsync(true);
        var foreignReservation = NewReservation("r1", "p2");
        _productReservationServiceMock.Setup(s => s.GetProductReservation("r1")).ReturnsAsync(foreignReservation);
        var model = new ProductModel.ReservationModel { ProductId = "p1", ReservationId = "r1" };

        var result = await _controller.ProductReservationDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productReservationServiceMock.Verify(
            s => s.DeleteProductReservation(It.IsAny<ProductReservation>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductReservationDelete_ReservationHasOrder_ReturnsErrorsJson_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var reservation = NewReservation("r1", "p1", "order1");
        _productReservationServiceMock.Setup(s => s.GetProductReservation("r1")).ReturnsAsync(reservation);
        var model = new ProductModel.ReservationModel { ProductId = "p1", ReservationId = "r1" };

        var result = await _controller.ProductReservationDelete(model);

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.IsNotNull(gridModel.Errors);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.ProductReservations.CantDeleteWithOrder"), Times.Once);
        _productReservationServiceMock.Verify(
            s => s.DeleteProductReservation(It.IsAny<ProductReservation>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductReservationDelete_ScopeGrantsAccess_SameProduct_Deletes()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var reservation = NewReservation("r1", "p1");
        _productReservationServiceMock.Setup(s => s.GetProductReservation("r1")).ReturnsAsync(reservation);
        var model = new ProductModel.ReservationModel { ProductId = "p1", ReservationId = "r1" };

        var result = await _controller.ProductReservationDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productReservationServiceMock.Verify(s => s.DeleteProductReservation(reservation), Times.Once);
    }

    [TestMethod]
    public async Task ProductReservationDelete_ReservationNotFound_ReturnsEmptyJson_NoOp()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _productReservationServiceMock.Setup(s => s.GetProductReservation("missing")).ReturnsAsync((ProductReservation)null);
        var model = new ProductModel.ReservationModel { ProductId = "p1", ReservationId = "missing" };

        var result = await _controller.ProductReservationDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _productReservationServiceMock.Verify(
            s => s.DeleteProductReservation(It.IsAny<ProductReservation>()), Times.Never);
    }
}
