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
    private IRepository<Customer> _customerRepository;
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
        var dateTimeServiceMock = new Mock<IDateTimeService>();
        dateTimeServiceMock.Setup(x => x.ConvertToUserTime(It.IsAny<DateTime>())).Returns<DateTime>(x => x);

        _service = new CustomerReportService(_customerRepository, _orderRepository, groupServiceMock.Object,
            dateTimeServiceMock.Object);
    }

    private Task InsertRegistered(string storeId)
    {
        var customer = new Customer { StoreId = storeId };
        customer.Groups.Add(_registered.Id);
        return _customerRepository.InsertAsync(customer);
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
        await InsertRegistered("store");
        await InsertRegistered("store");

        var result = await _service.GetCustomerByTimeReport("", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(2, result[0].Registered);
        Assert.AreEqual(DateTime.UtcNow.ToString("yyyy-MM-dd"), result[0].Time);
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
