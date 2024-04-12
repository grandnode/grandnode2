using Grand.Business.Common.Events;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Events;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Common.Tests.Events;

[TestClass]
public class GroupDeletedEventHandlerTests
{
    private MemoryCacheBase _cacheBase;
    private GroupDeletedEventHandler _handler;
    private IRepository<Product> _product;

    private IRepository<Customer> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Customer>();
        _product = new MongoDBRepositoryTest<Product>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), new Mock<IMediator>().Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });

        _handler = new GroupDeletedEventHandler(_repository, _product, _cacheBase);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var customer1 = new Customer();
        customer1.Groups.Add("1");
        customer1.Groups.Add("2");
        customer1.Groups.Add("3");
        await _repository.InsertAsync(customer1);
        var customer2 = new Customer();
        customer2.Groups.Add("1");
        customer2.Groups.Add("2");
        customer2.Groups.Add("3");
        await _repository.InsertAsync(customer2);
        //Act
        var notification = new EntityDeleted<CustomerGroup>(new CustomerGroup { Id = "1" });
        await _handler.Handle(notification, CancellationToken.None);

        //Assert
        var count = _repository.Table.Where(x => x.Groups.Contains("1")).Count();
        Assert.AreEqual(0, count);
    }
}