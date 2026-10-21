using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Orders;

[TestClass]
public class OrderReportServiceTests
{
    private IRepository<Order> _orderRepository;
    private IRepository<ProductAlsoPurchased> _productAlsoPurchasedRepository;
    private IRepository<Product> _productRepository;
    private OrderReportService _service;

    [TestInitialize]
    public void Init()
    {
        _orderRepository = new MongoDBRepositoryTest<Order>();
        _productRepository = new MongoDBRepositoryTest<Product>();
        _productAlsoPurchasedRepository = new MongoDBRepositoryTest<ProductAlsoPurchased>();
        _service = new OrderReportService(_orderRepository, _productAlsoPurchasedRepository, _productRepository,
            new Mock<IDateTimeService>().Object);
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
            await InsertOrder("store", Item($"p{i}", i, i * 10));

        var result = await _service.BestSellersReport(orderBy: 1, pageIndex: 1, pageSize: 2);

        CollectionAssert.AreEqual(new[] { "p3", "p2" }, result.Select(x => x.ProductId).ToArray());
        Assert.AreEqual(5, result.TotalCount);
        Assert.AreEqual(3, result.TotalPages);
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
}
