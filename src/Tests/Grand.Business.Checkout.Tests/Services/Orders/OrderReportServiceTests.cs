using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Utilities.System;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Orders;

[TestClass]
public class OrderReportServiceTests
{
    private MongoDBRepositoryTest<Order> _orderRepository;
    private IRepository<ProductAlsoPurchased> _productAlsoPurchasedRepository;
    private IRepository<Product> _productRepository;
    private MongoDBRepositoryTest<MerchandiseReturn> _merchandiseReturnRepository;
    private OrderReportService _service;

    public OrderReportServiceTests()
    {
        _orderRepository = new MongoDBRepositoryTest<Order>();
        _productRepository = new MongoDBRepositoryTest<Product>();
        _productAlsoPurchasedRepository = new MongoDBRepositoryTest<ProductAlsoPurchased>();
        _merchandiseReturnRepository = new MongoDBRepositoryTest<MerchandiseReturn>();
        _service = new OrderReportService(_orderRepository, _productAlsoPurchasedRepository, _productRepository,
            _merchandiseReturnRepository, new Mock<IDateTimeService>().Object);
    }

    private Task InsertOrder(string storeId, params OrderItem[] items)
    {
        var order = new Order { StoreId = storeId, Rate = 1, CurrencyRate = 1 };
        foreach (var item in items)
            order.OrderItems.Add(item);
        return _orderRepository.InsertAsync(order);
    }

    private static OrderItem Item(string productId, int quantity, double priceInclTax, string vendorId = "")
    {
        return new OrderItem {
            ProductId = productId, Quantity = quantity, PriceInclTax = priceInclTax, VendorId = vendorId
        };
    }

    [TestMethod]
    public async Task BestSellersReport_GroupsByProduct_OrderedByQuantity()
    {
        await InsertOrder("store", Item("p1", 1, 100), Item("p2", 3, 30));
        await InsertOrder("store", Item("p2", 2, 20), Item("p3", 4, 500));

        var result = await _service.BestSellersReport(orderBy: 1);

        CollectionAssert.AreEqual(new[] { "p2", "p3", "p1" }, result.Select(x => x.ProductId).ToArray());
        Assert.AreEqual(5, result[0].TotalQuantity);
        Assert.AreEqual(50, result[0].TotalAmount);
        Assert.AreEqual(3, result.TotalCount);
    }

    [TestMethod]
    public async Task BestSellersReport_OrderedByAmount()
    {
        await InsertOrder("store", Item("p1", 1, 100), Item("p2", 3, 30));
        await InsertOrder("store", Item("p3", 1, 500));

        var result = await _service.BestSellersReport(orderBy: 2);

        CollectionAssert.AreEqual(new[] { "p3", "p1", "p2" }, result.Select(x => x.ProductId).ToArray());
    }

    [TestMethod]
    public async Task BestSellersReport_ReturnsRequestedPage_WithTotalOverAllGroups()
    {
        for (var i = 1; i <= 5; i++)
            await InsertOrder("store", Item($"p{i}", i, i * 10d));

        var result = await _service.BestSellersReport(orderBy: 1, pageIndex: 1, pageSize: 2);

        CollectionAssert.AreEqual(new[] { "p3", "p2" }, result.Select(x => x.ProductId).ToArray());
        Assert.AreEqual(5, result.TotalCount);
        Assert.AreEqual(3, result.TotalPages);
    }

    [TestMethod]
    public async Task BestSellersReport_TiedProducts_PagesDoNotOverlap()
    {
        for (var i = 1; i <= 6; i++)
            await InsertOrder("store", Item($"p{i}", 1, 10));

        var pages = new List<string>();
        for (var page = 0; page < 3; page++)
            pages.AddRange((await _service.BestSellersReport(orderBy: 1, pageIndex: page, pageSize: 2))
                .Select(x => x.ProductId ?? ""));

        CollectionAssert.AreEqual(new[] { "p1", "p2", "p3", "p4", "p5", "p6" }, pages);
    }

    [TestMethod]
    public async Task BestSellersReport_AmountInPrimaryCurrency()
    {
        var order = new Order { CurrencyRate = 2, Rate = 8 };
        order.OrderItems.Add(Item("p1", 1, 40));
        await _orderRepository.InsertAsync(order);

        var result = await _service.BestSellersReport();

        Assert.AreEqual(20, result[0].TotalAmount);
    }

    [TestMethod]
    public async Task GetCountryReport_Vendor_CountsOrdersNotItems()
    {
        var order = new Order { CurrencyRate = 2, BillingAddress = new Address { CountryId = "pl" } };
        order.OrderItems.Add(Item("p1", 1, 10, "v1"));
        order.OrderItems.Add(Item("p2", 1, 30, "v1"));
        order.OrderItems.Add(Item("p3", 1, 99, "v2"));
        await _orderRepository.InsertAsync(order);

        var result = await _service.GetCountryReport(vendorId: "v1");

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("pl", result[0].CountryId);
        Assert.AreEqual(1, result[0].TotalOrders);
        Assert.AreEqual(20, result[0].SumOrders);
    }

    [TestMethod]
    public async Task GetOrderByTimeReport_PrimaryCurrency_SortedByDay_IncludesWholeEndDay()
    {
        //written to the collection directly, InsertAsync overwrites CreatedOnUtc
        var end = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc);
        await _orderRepository.Collection.InsertOneAsync(new Order {
            OrderTotal = 40, CurrencyRate = 2, CreatedOnUtc = end.AddHours(23).AddMinutes(59).AddSeconds(30)
        });
        await _orderRepository.Collection.InsertOneAsync(new Order {
            OrderTotal = 10, CurrencyRate = 1, CreatedOnUtc = end.AddDays(-2)
        });
        await _orderRepository.Collection.InsertOneAsync(new Order {
            OrderTotal = 10, CurrencyRate = 1, CreatedOnUtc = end.AddDays(1)
        });

        var result = await _service.GetOrderByTimeReport("", end.AddDays(-5), end);

        CollectionAssert.AreEqual(new[] { "2026-03-08", "2026-03-10" }, result.Select(x => x.Time).ToArray());
        Assert.AreEqual(20, result[1].SumOrders);
    }

    [TestMethod]
    public async Task BestSellersReport_VendorAndStoreFilters_CountOnlyMatchingItems()
    {
        await InsertOrder("store", Item("p1", 4, 40, "v1"), Item("p2", 9, 90, "v2"));
        await InsertOrder("other", Item("p1", 7, 70, "v1"));

        var result = await _service.BestSellersReport("store", "v1");

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("p1", result[0].ProductId);
        Assert.AreEqual(4, result[0].TotalQuantity);
    }

    [TestMethod]
    public async Task GetOrderAverageReportLine_NoOrders_ReturnsZeros()
    {
        var result = await _service.GetOrderAverageReportLine();

        Assert.AreEqual(0, result.CountOrders);
        Assert.AreEqual(0, result.SumOrders);
    }

    [TestMethod]
    public async Task GetOrderAverageReportLine_SumsOrdersInPrimaryCurrency()
    {
        await _orderRepository.InsertAsync(new Order { OrderTotal = 10, CurrencyRate = 1 });
        await _orderRepository.InsertAsync(new Order { OrderTotal = 30, CurrencyRate = 2 });

        var result = await _service.GetOrderAverageReportLine();

        Assert.AreEqual(2, result.CountOrders);
        Assert.AreEqual(25, result.SumOrders);
    }

    [TestMethod]
    public async Task ProductsNeverSold_ExcludesSoldProducts()
    {
        var sold = new Product { Name = "a", Published = true, ProductTypeId = ProductType.SimpleProduct };
        var unsold = new Product { Name = "b", Published = true, ProductTypeId = ProductType.SimpleProduct };
        await _productRepository.InsertAsync(sold);
        await _productRepository.InsertAsync(unsold);
        await InsertOrder("store", Item(sold.Id, 1, 10));
        await InsertOrder("store", Item(sold.Id, 2, 20));

        var result = await _service.ProductsNeverSold();

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(unsold.Id, result[0].Id);
    }

    [TestMethod]
    public async Task GetAlsoPurchasedProductsIds_OrderedByQuantity_LimitedToRecords()
    {
        await _productAlsoPurchasedRepository.InsertAsync(new ProductAlsoPurchased {
            ProductId = "p", ProductId2 = "a", Quantity = 1
        });
        await _productAlsoPurchasedRepository.InsertAsync(new ProductAlsoPurchased {
            ProductId = "p", ProductId2 = "b", Quantity = 5
        });
        await _productAlsoPurchasedRepository.InsertAsync(new ProductAlsoPurchased {
            ProductId = "p", ProductId2 = "c", Quantity = 3
        });

        var result = await _service.GetAlsoPurchasedProductsIds("", "p", 2);

        CollectionAssert.AreEqual(new[] { "b", "c" }, result);
    }

    // --- Merchandise return report ---------------------------------------------------------------

    private static readonly DateTime From = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime To = new(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime InRange = new(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc);

    //the repository stamps the insert time: the test's date is written after it
    private async Task<Order> SoldOrder(DateTime createdOnUtc, string storeId = "store", bool deleted = false,
        int status = (int)OrderStatusSystem.Complete, params OrderItem[] items)
    {
        var order = new Order { StoreId = storeId, Deleted = deleted, OrderStatusId = status };
        foreach (var item in items)
            order.OrderItems.Add(item);
        await _orderRepository.InsertAsync(order);
        await _orderRepository.UpdateOneAsync(x => x.Id == order.Id,
            UpdateBuilder<Order>.Create().Set(x => x.CreatedOnUtc, createdOnUtc));
        return order;
    }

    private async Task<MerchandiseReturn> Returned(Order order, DateTime createdOnUtc,
        MerchandiseReturnStatus status = MerchandiseReturnStatus.Received, string vendorId = "",
        params MerchandiseReturnItem[] items)
    {
        var merchandiseReturn = new MerchandiseReturn {
            OrderId = order.Id, StoreId = order.StoreId, VendorId = vendorId, MerchandiseReturnStatus = status
        };
        foreach (var item in items)
            merchandiseReturn.MerchandiseReturnItems.Add(item);
        await _merchandiseReturnRepository.InsertAsync(merchandiseReturn);
        await _merchandiseReturnRepository.UpdateOneAsync(x => x.Id == merchandiseReturn.Id,
            UpdateBuilder<MerchandiseReturn>.Create().Set(x => x.CreatedOnUtc, createdOnUtc));
        return merchandiseReturn;
    }

    private static MerchandiseReturnItem ReturnItem(OrderItem orderItem, int quantity, string? reasonId = null,
        string reasonText = "Damaged") =>
        new() { OrderItemId = orderItem.Id, Quantity = quantity, ReasonId = reasonId, ReasonForReturn = reasonText };

    [TestMethod]
    public async Task ReturnReport_ByReason_IdAndLegacyText()
    {
        var item = Item("p1", 5, 10);
        var order = await SoldOrder(InRange, items: item);
        await Returned(order, InRange, items: ReturnItem(item, 2, "r1"));
        await Returned(order, InRange, items: ReturnItem(item, 1, "r1"));
        await Returned(order, InRange, items: ReturnItem(item, 1, null, " Too small "));

        var lines = await _service.MerchandiseReturnReport(MerchandiseReturnReportGrouping.Reason, From, To);

        Assert.AreEqual(2, lines.Count);
        Assert.AreEqual("r1", lines[0].Key);
        Assert.AreEqual(2, lines[0].ReturnCount);
        Assert.AreEqual(3, lines[0].Quantity);
        Assert.AreEqual("Too small", lines[1].Key);
        Assert.AreEqual("Too small", lines[1].Name);
        Assert.AreEqual(1, lines[1].ReturnCount);
        Assert.AreEqual(1, lines[1].Quantity);
        Assert.IsNull(lines[0].SoldQuantity);
    }

    [TestMethod]
    public async Task ReturnReport_RejectedAndCancelledNotCounted()
    {
        var item = Item("p1", 5, 10);
        var order = await SoldOrder(InRange, items: item);
        await Returned(order, InRange, MerchandiseReturnStatus.Rejected, items: ReturnItem(item, 1, "r1"));
        await Returned(order, InRange, MerchandiseReturnStatus.Cancelled, items: ReturnItem(item, 1, "r1"));
        await Returned(order, InRange, MerchandiseReturnStatus.Pending, items: ReturnItem(item, 1, "r1"));

        var lines = await _service.MerchandiseReturnReport(MerchandiseReturnReportGrouping.Reason, From, To);

        Assert.AreEqual(1, lines.Single().Quantity);
    }

    [TestMethod]
    public async Task ReturnReport_OutsideRangeNotCounted()
    {
        var item = Item("p1", 5, 10);
        var order = await SoldOrder(InRange, items: item);
        await Returned(order, From.AddDays(-1), items: ReturnItem(item, 1, "r1"));
        await Returned(order, To.AddDays(1), items: ReturnItem(item, 1, "r1"));

        var lines = await _service.MerchandiseReturnReport(MerchandiseReturnReportGrouping.Reason, From, To);

        Assert.AreEqual(0, lines.Count);
    }

    [TestMethod]
    public async Task ReturnReport_ByVendor_StoreItemsUnderEmptyKey()
    {
        var own = Item("p1", 5, 10);
        var vendors = Item("p2", 5, 10, "v1");
        var order = await SoldOrder(InRange, items: [own, vendors]);
        await Returned(order, InRange, items: ReturnItem(own, 2, "r1"));
        await Returned(order, InRange, vendorId: "v1", items: ReturnItem(vendors, 3, "r1"));

        var lines = await _service.MerchandiseReturnReport(MerchandiseReturnReportGrouping.Vendor, From, To);

        Assert.AreEqual("v1", lines[0].Key);
        Assert.AreEqual(3, lines[0].Quantity);
        Assert.AreEqual("", lines[1].Key);
        Assert.AreEqual(2, lines[1].Quantity);
    }

    [TestMethod]
    public async Task ReturnReport_ByProduct_SoldInSamePeriod()
    {
        var sold = Item("p1", 10, 10);
        var order = await SoldOrder(InRange, items: sold);
        await SoldOrder(From.AddDays(-5), items: Item("p1", 5, 10));
        await SoldOrder(InRange, status: (int)OrderStatusSystem.Cancelled, items: Item("p1", 3, 10));
        await SoldOrder(InRange, deleted: true, items: Item("p1", 2, 10));
        await Returned(order, InRange, items: ReturnItem(sold, 2, "r1"));

        var line = (await _service.MerchandiseReturnReport(MerchandiseReturnReportGrouping.Product, From, To))
            .Single();

        Assert.AreEqual("p1", line.Key);
        Assert.AreEqual(1, line.ReturnCount);
        Assert.AreEqual(2, line.Quantity);
        Assert.AreEqual(10, line.SoldQuantity);
    }

    [TestMethod]
    public async Task ReturnReport_StoreScope()
    {
        var item = Item("p1", 4, 10);
        var order = await SoldOrder(InRange, items: item);
        var other = Item("p1", 6, 10);
        var otherOrder = await SoldOrder(InRange, "store2", items: other);
        await Returned(order, InRange, items: ReturnItem(item, 1, "r1"));
        await Returned(otherOrder, InRange, items: ReturnItem(other, 5, "r1"));

        var line = (await _service.MerchandiseReturnReport(MerchandiseReturnReportGrouping.Product, From, To,
            storeId: "store")).Single();

        Assert.AreEqual(1, line.Quantity);
        Assert.AreEqual(4, line.SoldQuantity);
    }

    [TestMethod]
    public async Task ReturnReport_VendorScope_SoldOnlyVendorItems()
    {
        var own = Item("p1", 7, 10);
        var vendors = Item("p1", 2, 10, "v1");
        var order = await SoldOrder(InRange, items: [own, vendors]);
        await Returned(order, InRange, items: ReturnItem(own, 3, "r1"));
        await Returned(order, InRange, vendorId: "v1", items: ReturnItem(vendors, 1, "r1"));

        var line = (await _service.MerchandiseReturnReport(MerchandiseReturnReportGrouping.Product, From, To,
            vendorId: "v1")).Single();

        Assert.AreEqual(1, line.Quantity);
        Assert.AreEqual(2, line.SoldQuantity);
    }

    [TestMethod]
    public async Task ReturnReport_Paging_QuantityDescThenKey()
    {
        var item = Item("p1", 9, 10);
        var order = await SoldOrder(InRange, items: item);
        await Returned(order, InRange, items: ReturnItem(item, 1, "b"));
        await Returned(order, InRange, items: ReturnItem(item, 1, "a"));
        await Returned(order, InRange, items: ReturnItem(item, 3, "c"));

        var page = await _service.MerchandiseReturnReport(MerchandiseReturnReportGrouping.Reason, From, To,
            pageIndex: 1, pageSize: 1);

        Assert.AreEqual(3, page.TotalCount);
        Assert.AreEqual("a", page.Single().Key);
    }
}
