using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Customers.Services;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Customers.Tests.Services;

[TestClass]
public class CustomerReportServiceTests
{
    private readonly CustomerGroup _registered = new() { SystemName = SystemCustomerGroupNames.Registered };
    private MongoDBRepositoryTest<Customer> _customerRepository;
    private IRepository<Order> _orderRepository;
    private CustomerReportService _service;

    [TestInitialize]
    public void Init()
    {
        _customerRepository = new MongoDBRepositoryTest<Customer>();
        _orderRepository = new MongoDBRepositoryTest<Order>();

        var groupServiceMock = new Mock<IGroupService>();
        groupServiceMock.Setup(x => x.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Registered))
            .ReturnsAsync(_registered);
        _service = new CustomerReportService(_customerRepository, _orderRepository, groupServiceMock.Object);
    }

    private Task InsertRegistered(string storeId, DateTime? createdOnUtc = null)
    {
        var customer = new Customer { StoreId = storeId };
        customer.Groups.Add(_registered.Id);
        if (createdOnUtc == null)
            return _customerRepository.InsertAsync(customer);

        //written to the collection directly, InsertAsync overwrites CreatedOnUtc
        customer.CreatedOnUtc = createdOnUtc.Value;
        return _customerRepository.Collection.InsertOneAsync(customer);
    }

    [TestMethod]
    public async Task GetRegisteredCustomersReport_CountsRegisteredInStore()
    {
        await InsertRegistered("store");
        await InsertRegistered("store");
        await InsertRegistered("other");
        await _customerRepository.InsertAsync(new Customer { StoreId = "store" });

        var result = await _service.GetRegisteredCustomersReport("store", 7);

        Assert.AreEqual(2, result);
    }

    [TestMethod]
    public async Task GetCustomerByTimeReport_GroupsRegisteredByDay()
    {
        var end = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc);
        await InsertRegistered("store", end.AddHours(23).AddMinutes(59).AddSeconds(30));
        await InsertRegistered("store", end.AddHours(1));
        await InsertRegistered("store", end.AddDays(-2));
        await InsertRegistered("store", end.AddDays(1));

        var result = await _service.GetCustomerByTimeReport("", end.AddDays(-5), end);

        CollectionAssert.AreEqual(new[] { "2026-03-08", "2026-03-10" }, result.Select(x => x.Time).ToArray());
        Assert.AreEqual(2, result[1].Registered);
    }

    [TestMethod]
    public async Task GetCustomerByTimeReport_NoRegisteredGroup_ReturnsEmpty()
    {
        var service = new CustomerReportService(_customerRepository, _orderRepository, new Mock<IGroupService>().Object);

        var result = await service.GetCustomerByTimeReport("");

        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task GetBestCustomersReport_Vendor_CountsOrdersNotItems()
    {
        var order = new Order { CustomerId = "c1", CurrencyRate = 2 };
        order.OrderItems.Add(new OrderItem { VendorId = "v1", PriceInclTax = 10 });
        order.OrderItems.Add(new OrderItem { VendorId = "v1", PriceInclTax = 30 });
        order.OrderItems.Add(new OrderItem { VendorId = "v2", PriceInclTax = 99 });
        await _orderRepository.InsertAsync(order);

        var result = await _service.GetBestCustomersReport(vendorId: "v1", orderBy: 1);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(1, result[0].OrderCount);
        Assert.AreEqual(20, result[0].OrderTotal);
    }

    [TestMethod]
    public async Task GetBestCustomersReport_OrderedByTotal_Paged()
    {
        await _orderRepository.InsertAsync(new Order { CustomerId = "c1", OrderTotal = 10, CurrencyRate = 1 });
        await _orderRepository.InsertAsync(new Order { CustomerId = "c2", OrderTotal = 50, CurrencyRate = 1 });
        await _orderRepository.InsertAsync(new Order { CustomerId = "c1", OrderTotal = 20, CurrencyRate = 1 });
        await _orderRepository.InsertAsync(new Order { CustomerId = "c3", OrderTotal = 5, CurrencyRate = 1 });

        var result = await _service.GetBestCustomersReport(orderBy: 1, pageIndex: 0, pageSize: 2);

        CollectionAssert.AreEqual(new[] { "c2", "c1" }, result.Select(x => x.CustomerId).ToArray());
        Assert.AreEqual(30, result[1].OrderTotal);
        Assert.AreEqual(2, result[1].OrderCount);
        Assert.AreEqual(3, result.TotalCount);
    }
}
