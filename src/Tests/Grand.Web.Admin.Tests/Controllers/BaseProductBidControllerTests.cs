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
public class BaseProductBidControllerTests : ProductControllerTestsBase<BaseProductBidControllerTests.TestProductBidController>
{
    // BaseProductBidController is abstract. This minimal subclass exists only so the actions under test
    // can be invoked directly.
    public class TestProductBidController(
        IProductBidViewModelService productBidViewModelService,
        IProductService productService,
        IRequestTranslationService translationService,
        IAuctionService auctionService,
        IAdminDataScope<Product> scope)
        : BaseProductBidController(productBidViewModelService, productService, translationService, auctionService, scope);

    protected override TestProductBidController CreateController() => new(
        _productBidViewModelServiceMock.Object,
        _productServiceMock.Object,
        _translationServiceMock.Object,
        _auctionServiceMock.Object,
        _scopeMock.Object);

    private static Bid NewBid(string id, string productId, string orderId = "") =>
        new() { Id = id, ProductId = productId, OrderId = orderId, Date = DateTime.UtcNow };

    // --- ListBids --------------------------------------------------------------------------------

    [TestMethod]
    public async Task ListBids_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing")).ReturnsAsync((Product)null);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.ListBids(new Grand.Web.Common.DataSource.DataSourceRequest(), "missing"));
    }

    [TestMethod]
    public async Task ListBids_ScopeDeniesAccess_ReturnsKendoGridError_DoesNotQuery()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(false);

        var result = await _controller.ListBids(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _productBidViewModelServiceMock.Verify(
            s => s.PrepareBidMode(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public async Task ListBids_ScopeGrantsAccess_ReturnsBids()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _productBidViewModelServiceMock.Setup(s => s.PrepareBidMode("p1", 0, 10))
            .ReturnsAsync((Enumerable.Empty<ProductModel.BidModel>(), 0));

        var result = await _controller.ListBids(
            new Grand.Web.Common.DataSource.DataSourceRequest { PageSize = 10 }, "p1");

        Assert.IsInstanceOfType<JsonResult>(result);
        var json = result as JsonResult;
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(0, gridModel.Total);
    }

    // A product shared with other stores opens read-only on Store: the tab loads, but the bids
    // (other stores' customers) are not listed.
    [TestMethod]
    public async Task ListBids_ViewableButNotOwned_ReturnsEmptyGrid_DoesNotQuery()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.CanView(product)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);

        var result = await _controller.ListBids(new Grand.Web.Common.DataSource.DataSourceRequest(), "p1");

        var gridModel = (result as JsonResult)?.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual(0, gridModel.Total);
        _translationServiceMock.Verify(t => t.GetResource(It.IsAny<string>()), Times.Never);
        _productBidViewModelServiceMock.Verify(
            s => s.PrepareBidMode(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    // --- BidDelete ---------------------------------------------------------------------------------

    [TestMethod]
    public async Task BidDelete_MissingProduct_Throws()
    {
        _productServiceMock.Setup(p => p.GetProductById("missing")).ReturnsAsync((Product)null);
        var model = new ProductModel.BidModel { ProductId = "missing", BidId = "b1" };

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _controller.BidDelete(model));
    }

    [TestMethod]
    public async Task BidDelete_ScopeDeniesAccess_ReturnsKendoGridError_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(false);
        var model = new ProductModel.BidModel { ProductId = "p1", BidId = "b1" };

        var result = await _controller.BidDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _auctionServiceMock.Verify(s => s.DeleteBid(It.IsAny<Bid>()), Times.Never);
    }

    // Regression guard for the same cross-product IDOR shape as ProductReservationDelete above (found by
    // the row 23 reviewer to apply identically here): the access check ran against model.ProductId while
    // the delete ran against model.BidId, with no check that the two referred to the same product. A
    // caller with access to *any* product could pass that product's id for the access check while
    // supplying the BidId of a bid belonging to a different, unowned product, deleting it.
    [TestMethod]
    public async Task BidDelete_BidBelongsToDifferentProduct_ReturnsKendoGridError_DoesNotDelete()
    {
        var ownedProduct = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(ownedProduct);
        _scopeMock.Setup(s => s.HasAccess(ownedProduct)).ReturnsAsync(true);
        var foreignBid = NewBid("b1", "p2");
        _auctionServiceMock.Setup(s => s.GetBid("b1")).ReturnsAsync(foreignBid);
        var model = new ProductModel.BidModel { ProductId = "p1", BidId = "b1" };

        var result = await _controller.BidDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Permissions"), Times.Once);
        _auctionServiceMock.Verify(s => s.DeleteBid(It.IsAny<Bid>()), Times.Never);
    }

    [TestMethod]
    public async Task BidDelete_BidHasOrder_ReturnsErrorsJson_DoesNotDelete()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var bid = NewBid("b1", "p1", "order1");
        _auctionServiceMock.Setup(s => s.GetBid("b1")).ReturnsAsync(bid);
        var model = new ProductModel.BidModel { ProductId = "p1", BidId = "b1" };

        var result = await _controller.BidDelete(model);

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.IsNotNull(gridModel.Errors);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Catalog.Products.Bids.CantDeleteWithOrder"), Times.Once);
        _auctionServiceMock.Verify(s => s.DeleteBid(It.IsAny<Bid>()), Times.Never);
    }

    [TestMethod]
    public async Task BidDelete_ScopeGrantsAccess_SameProduct_Deletes()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        var bid = NewBid("b1", "p1");
        _auctionServiceMock.Setup(s => s.GetBid("b1")).ReturnsAsync(bid);
        var model = new ProductModel.BidModel { ProductId = "p1", BidId = "b1" };

        var result = await _controller.BidDelete(model);

        Assert.IsInstanceOfType<JsonResult>(result);
        _auctionServiceMock.Verify(s => s.DeleteBid(bid), Times.Once);
    }

    [TestMethod]
    public async Task BidDelete_BidNotFound_ReturnsErrorsJson_NoOp()
    {
        var product = new Product { Id = "p1" };
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);
        _scopeMock.Setup(s => s.HasAccess(product)).ReturnsAsync(true);
        _auctionServiceMock.Setup(s => s.GetBid("missing")).ReturnsAsync((Bid)null);
        var model = new ProductModel.BidModel { ProductId = "p1", BidId = "missing" };

        var result = await _controller.BidDelete(model);

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = json.Value as Grand.Web.Common.DataSource.DataSourceResult;
        Assert.IsNotNull(gridModel);
        Assert.AreEqual("Bid not exists", gridModel.Errors);
        _auctionServiceMock.Verify(s => s.DeleteBid(It.IsAny<Bid>()), Times.Never);
    }
}
