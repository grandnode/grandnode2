using Grand.Business.Checkout.Events.Cache;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Events.Cache;

[TestClass]
public class CheckoutCacheEventHandlersTests
{
    [TestMethod]
    public async Task CheckoutAttribute_InsertedCheckoutAttribute_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<CheckoutAttribute>(new CheckoutAttribute());
        await new CheckoutAttributeCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.CHECKOUTATTRIBUTES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.CHECKOUTATTRIBUTEVALUES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task CheckoutAttribute_UpdatedCheckoutAttribute_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<CheckoutAttribute>(new CheckoutAttribute());
        await new CheckoutAttributeCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.CHECKOUTATTRIBUTES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.CHECKOUTATTRIBUTEVALUES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task CheckoutAttribute_DeletedCheckoutAttribute_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<CheckoutAttribute>(new CheckoutAttribute());
        await new CheckoutAttributeCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.CHECKOUTATTRIBUTES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.CHECKOUTATTRIBUTEVALUES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task MerchandiseReturn_InsertedMerchandiseReturnAction_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<MerchandiseReturnAction>(new MerchandiseReturnAction());
        await new MerchandiseReturnCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.MERCHANDISE_RETURN_ACTIONS_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task MerchandiseReturn_UpdatedMerchandiseReturnAction_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<MerchandiseReturnAction>(new MerchandiseReturnAction());
        await new MerchandiseReturnCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.MERCHANDISE_RETURN_ACTIONS_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task MerchandiseReturn_DeletedMerchandiseReturnAction_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<MerchandiseReturnAction>(new MerchandiseReturnAction());
        await new MerchandiseReturnCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.MERCHANDISE_RETURN_ACTIONS_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task MerchandiseReturn_InsertedMerchandiseReturnReason_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<MerchandiseReturnReason>(new MerchandiseReturnReason());
        await new MerchandiseReturnCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.MERCHANDISE_RETURN_REASONS_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task MerchandiseReturn_UpdatedMerchandiseReturnReason_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<MerchandiseReturnReason>(new MerchandiseReturnReason());
        await new MerchandiseReturnCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.MERCHANDISE_RETURN_REASONS_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task MerchandiseReturn_DeletedMerchandiseReturnReason_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<MerchandiseReturnReason>(new MerchandiseReturnReason());
        await new MerchandiseReturnCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.MERCHANDISE_RETURN_REASONS_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task OrderStatus_InsertedOrderStatus_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<OrderStatus>(new OrderStatus());
        await new OrderStatusCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.ORDER_STATUS_ALL, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task OrderStatus_UpdatedOrderStatus_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<OrderStatus>(new OrderStatus());
        await new OrderStatusCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.ORDER_STATUS_ALL, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task OrderStatus_DeletedOrderStatus_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<OrderStatus>(new OrderStatus());
        await new OrderStatusCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.ORDER_STATUS_ALL, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task DeliveryDate_InsertedDeliveryDate_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<DeliveryDate>(new DeliveryDate());
        await new DeliveryDateCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.DELIVERYDATE_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task DeliveryDate_UpdatedDeliveryDate_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<DeliveryDate>(new DeliveryDate());
        await new DeliveryDateCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.DELIVERYDATE_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task DeliveryDate_DeletedDeliveryDate_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<DeliveryDate>(new DeliveryDate());
        await new DeliveryDateCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.DELIVERYDATE_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task PickupPoint_InsertedPickupPoint_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<PickupPoint>(new PickupPoint());
        await new PickupPointCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.PICKUPPOINTS_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task PickupPoint_UpdatedPickupPoint_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<PickupPoint>(new PickupPoint());
        await new PickupPointCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.PICKUPPOINTS_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task PickupPoint_DeletedPickupPoint_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<PickupPoint>(new PickupPoint());
        await new PickupPointCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.PICKUPPOINTS_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ShippingMethod_InsertedShippingMethod_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<ShippingMethod>(new ShippingMethod());
        await new ShippingMethodCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.SHIPPINGMETHOD_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ShippingMethod_UpdatedShippingMethod_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<ShippingMethod>(new ShippingMethod());
        await new ShippingMethodCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.SHIPPINGMETHOD_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ShippingMethod_DeletedShippingMethod_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<ShippingMethod>(new ShippingMethod());
        await new ShippingMethodCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.SHIPPINGMETHOD_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Warehouse_InsertedWarehouse_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityInserted<Warehouse>(new Warehouse());
        await new WarehouseCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.WAREHOUSES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Warehouse_UpdatedWarehouse_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityUpdated<Warehouse>(new Warehouse());
        await new WarehouseCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.WAREHOUSES_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Warehouse_DeletedWarehouse_ClearsItsCache()
    {
        var cache = new Mock<ICacheBase>();

        var notification = new EntityDeleted<Warehouse>(new Warehouse());
        await new WarehouseCacheEventHandler(cache.Object).Handle(notification, CancellationToken.None);

        cache.Verify(c => c.RemoveByPrefix(CacheKey.WAREHOUSES_PATTERN_KEY, true), Times.Once);
        cache.Verify(c => c.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY, true), Times.Once);
        cache.VerifyNoOtherCalls();
    }
}
