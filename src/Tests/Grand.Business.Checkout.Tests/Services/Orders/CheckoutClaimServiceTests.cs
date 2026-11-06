using Grand.Business.Checkout.Extensions;
using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Discounts;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Orders;

[TestClass]
public class CheckoutClaimServiceTests
{
    private Mock<IDiscountService> _discountServiceMock;
    private Mock<IGiftVoucherService> _giftVoucherServiceMock;
    private Mock<IInventoryManageService> _inventoryManageServiceMock;
    private Mock<ILoyaltyPointsService> _loyaltyPointsServiceMock;
    private Mock<IProductService> _productServiceMock;
    private CheckoutClaimService _service;
    private Product _product;
    private Discount _discount;
    private List<CustomAttribute> _attributes;
    private PlaceOrderContainer _details;

    [TestInitialize]
    public void Init()
    {
        _inventoryManageServiceMock = new Mock<IInventoryManageService>();
        _discountServiceMock = new Mock<IDiscountService>();
        _giftVoucherServiceMock = new Mock<IGiftVoucherService>();
        _loyaltyPointsServiceMock = new Mock<ILoyaltyPointsService>();
        _productServiceMock = new Mock<IProductService>();

        _product = new Product { Id = "p1" };
        _productServiceMock.Setup(x => x.GetProductById("p1", false)).ReturnsAsync(_product);
        _discount = new Discount { Id = "d1" };
        _discountServiceMock.Setup(x => x.GetDiscountById("d1")).ReturnsAsync(_discount);

        _inventoryManageServiceMock.Setup(x => x.TryReserve(It.IsAny<Product>(), It.IsAny<int>(),
            It.IsAny<IList<CustomAttribute>>(), It.IsAny<string>())).ReturnsAsync(true);
        _discountServiceMock.Setup(x => x.TryInsertDiscountUsageHistory(It.IsAny<Discount>(),
            It.IsAny<DiscountUsageHistory>())).ReturnsAsync(true);
        _giftVoucherServiceMock.Setup(x => x.TryRedeem(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _loyaltyPointsServiceMock.Setup(x => x.TryRedeemLoyaltyPoints(It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()))
            .ReturnsAsync(new LoyaltyPointsHistory());

        _attributes = [new CustomAttribute { Key = "a", Value = "b" }];
        _details = new PlaceOrderContainer {
            OrderId = "o1",
            Customer = new Customer { Id = "c1" },
            Store = new Store { Id = "s1" },
            Cart = [new ShoppingCartItem { ProductId = "p1", Quantity = 2, WarehouseId = "w1", Attributes = _attributes }],
            AppliedDiscounts = [new ApplyDiscount { DiscountId = "d1", CouponCode = "COUPON" }],
            AppliedGiftVouchers = [
                new AppliedGiftVoucher { GiftVoucher = new GiftVoucher { Id = "g1" }, AmountCanBeUsed = 30 }
            ],
            RedeemedLoyaltyPoints = 100,
            RedeemedLoyaltyPointsAmount = 10
        };

        _service = new CheckoutClaimService(_inventoryManageServiceMock.Object, _discountServiceMock.Object,
            _giftVoucherServiceMock.Object, _loyaltyPointsServiceMock.Object, _productServiceMock.Object,
            Mock.Of<ILogger<CheckoutClaimService>>());
    }

    [TestMethod]
    public async Task Claim_EverythingAvailable_Succeeds()
    {
        var claim = await _service.Claim(_details, "CODE1");

        Assert.IsTrue(claim.Succeeded);
        Assert.AreEqual("o1", claim.OrderId);
        _inventoryManageServiceMock.Verify(x => x.TryReserve(_product, 2, _attributes, "w1"), Times.Once);
        _discountServiceMock.Verify(x => x.TryInsertDiscountUsageHistory(_discount,
            It.Is<DiscountUsageHistory>(h => h.DiscountId == "d1" && h.CouponCode == "COUPON" &&
                                             h.OrderId == "o1" && h.CustomerId == "c1")), Times.Once);
        _giftVoucherServiceMock.Verify(x => x.TryRedeem("g1", 30, "o1"), Times.Once);
        _loyaltyPointsServiceMock.Verify(x => x.TryRedeemLoyaltyPoints("c1", 100, "s1",
            "Loyalty points redeemed for order CODE1", "o1", 10), Times.Once);
        _inventoryManageServiceMock.Verify(x => x.CancelReservation(It.IsAny<Product>(), It.IsAny<int>(),
            It.IsAny<IList<CustomAttribute>>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Claim_VoucherRefused_ReleasesStockAndDiscountOnly()
    {
        _giftVoucherServiceMock.Setup(x => x.TryRedeem("g1", 30, "o1")).ReturnsAsync(false);

        var claim = await _service.Claim(_details, "CODE1");

        Assert.IsFalse(claim.Succeeded);
        _inventoryManageServiceMock.Verify(x => x.CancelReservation(_product, 2, _attributes, "w1"), Times.Once);
        _discountServiceMock.Verify(x => x.ReleaseDiscountUsageHistory(
            It.Is<DiscountUsageHistory>(h => h.DiscountId == "d1" && h.OrderId == "o1")), Times.Once);
        _giftVoucherServiceMock.Verify(x => x.CancelRedemption(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        _loyaltyPointsServiceMock.Verify(x => x.TryRedeemLoyaltyPoints(It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()), Times.Never);
        _loyaltyPointsServiceMock.Verify(x => x.AddLoyaltyPointsHistory(It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()), Times.Never);
    }

    [TestMethod]
    public async Task Claim_StockRefused_TouchesNothingElse()
    {
        _inventoryManageServiceMock.Setup(x => x.TryReserve(_product, 2, _attributes, "w1")).ReturnsAsync(false);

        var claim = await _service.Claim(_details, "CODE1");

        Assert.IsFalse(claim.Succeeded);
        _discountServiceMock.Verify(x => x.TryInsertDiscountUsageHistory(It.IsAny<Discount>(),
            It.IsAny<DiscountUsageHistory>()), Times.Never);
        _giftVoucherServiceMock.Verify(x => x.TryRedeem(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<string>()),
            Times.Never);
        _loyaltyPointsServiceMock.Verify(x => x.TryRedeemLoyaltyPoints(It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()), Times.Never);
        _inventoryManageServiceMock.Verify(x => x.CancelReservation(It.IsAny<Product>(), It.IsAny<int>(),
            It.IsAny<IList<CustomAttribute>>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Claim_SecondLineRefused_ReleasesFirstLine()
    {
        var other = new Product { Id = "p2" };
        _productServiceMock.Setup(x => x.GetProductById("p2", false)).ReturnsAsync(other);
        _inventoryManageServiceMock.Setup(x => x.TryReserve(other, 1, It.IsAny<IList<CustomAttribute>>(),
            It.IsAny<string>())).ReturnsAsync(false);
        _details.Cart.Add(new ShoppingCartItem { ProductId = "p2", Quantity = 1 });

        var claim = await _service.Claim(_details, "CODE1");

        Assert.IsFalse(claim.Succeeded);
        _inventoryManageServiceMock.Verify(x => x.CancelReservation(_product, 2, _attributes, "w1"), Times.Once);
        _inventoryManageServiceMock.Verify(x => x.CancelReservation(other, It.IsAny<int>(),
            It.IsAny<IList<CustomAttribute>>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Claim_NoPointsRedeemed_DoesNotTouchPoints()
    {
        _details.RedeemedLoyaltyPoints = 0;
        _details.RedeemedLoyaltyPointsAmount = 0;

        var claim = await _service.Claim(_details, "CODE1");

        Assert.IsTrue(claim.Succeeded);
        _loyaltyPointsServiceMock.Verify(x => x.TryRedeemLoyaltyPoints(It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()), Times.Never);
    }

    [TestMethod]
    public async Task Claim_DiscountNoLongerExists_IsNotRecorded()
    {
        _discountServiceMock.Setup(x => x.GetDiscountById("d1")).ReturnsAsync((Discount)null);

        var claim = await _service.Claim(_details, "CODE1");

        Assert.IsTrue(claim.Succeeded);
        _discountServiceMock.Verify(x => x.TryInsertDiscountUsageHistory(It.IsAny<Discount>(),
            It.IsAny<DiscountUsageHistory>()), Times.Never);
    }

    [TestMethod]
    public async Task Claim_ResourceBusyForTheWholeWait_RefusesAndReleases()
    {
        _giftVoucherServiceMock.Setup(x => x.TryRedeem("g1", 30, "o1"))
            .ThrowsAsync(new TimeoutException("The resource 'GiftVoucher:g1' is in use by another operation"));

        var claim = await _service.Claim(_details, "CODE1");

        Assert.IsFalse(claim.Succeeded);
        _inventoryManageServiceMock.Verify(x => x.CancelReservation(_product, 2, _attributes, "w1"), Times.Once);
        _discountServiceMock.Verify(x => x.ReleaseDiscountUsageHistory(It.IsAny<DiscountUsageHistory>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Release_ReturnsEveryClaimedResource()
    {
        var claim = await _service.Claim(_details, "CODE1");

        await _service.Release(claim);

        _inventoryManageServiceMock.Verify(x => x.CancelReservation(_product, 2, _attributes, "w1"), Times.Once);
        _discountServiceMock.Verify(x => x.ReleaseDiscountUsageHistory(
            It.Is<DiscountUsageHistory>(h => h.DiscountId == "d1" && h.OrderId == "o1")), Times.Once);
        _giftVoucherServiceMock.Verify(x => x.CancelRedemption("g1", "o1"), Times.Once);
        _loyaltyPointsServiceMock.Verify(x => x.AddLoyaltyPointsHistory("c1", 100, "s1",
            "Loyalty points returned for order CODE1", It.IsAny<string>(), It.IsAny<double>()), Times.Once);
    }

    [TestMethod]
    public async Task Release_OneStepThrows_OthersStillRun()
    {
        _giftVoucherServiceMock.Setup(x => x.CancelRedemption("g1", "o1")).ThrowsAsync(new Exception("down"));
        var claim = await _service.Claim(_details, "CODE1");

        await _service.Release(claim);

        _inventoryManageServiceMock.Verify(x => x.CancelReservation(_product, 2, _attributes, "w1"), Times.Once);
        _discountServiceMock.Verify(x => x.ReleaseDiscountUsageHistory(It.IsAny<DiscountUsageHistory>()),
            Times.Once);
        _loyaltyPointsServiceMock.Verify(x => x.AddLoyaltyPointsHistory("c1", 100, "s1", It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<double>()), Times.Once);
    }

    [TestMethod]
    public void OrderItemWarehouse_Resolve_CartItemWarehouseWins()
    {
        var warehouseId = OrderItemWarehouse.Resolve(new ShoppingCartItem { WarehouseId = "w1" },
            new Product { WarehouseId = "wp" }, "ws");

        Assert.AreEqual("w1", warehouseId);
    }

    [TestMethod]
    public void OrderItemWarehouse_Resolve_StoreDefaultWhenCartItemHasNone()
    {
        var warehouseId = OrderItemWarehouse.Resolve(new ShoppingCartItem(), new Product { WarehouseId = "wp" },
            "ws");

        Assert.AreEqual("ws", warehouseId);
    }

    [TestMethod]
    public void OrderItemWarehouse_Resolve_ProductWarehouseWhenSingleWarehouse()
    {
        var warehouseId = OrderItemWarehouse.Resolve(new ShoppingCartItem(), new Product { WarehouseId = "wp" },
            "");

        Assert.AreEqual("wp", warehouseId);
    }

    [TestMethod]
    public void OrderItemWarehouse_Resolve_NoFallbackForMultipleWarehouses()
    {
        var warehouseId = OrderItemWarehouse.Resolve(new ShoppingCartItem(),
            new Product { WarehouseId = "wp", UseMultipleWarehouses = true }, "");

        Assert.IsTrue(string.IsNullOrEmpty(warehouseId));
    }
}
