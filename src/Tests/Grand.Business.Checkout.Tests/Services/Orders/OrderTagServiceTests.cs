using Grand.Business.Checkout.Services.Orders;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Orders;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Orders;

[TestClass]
public class OrderTagServiceTests
{
    private MemoryCacheBase _cacheBase;
    private Mock<IMediator> _mediatorMock;
    private IRepository<Order> _orderRepository;
    private IRepository<OrderTag> _orderTagRepository;
    private OrderTagService _service;

    [TestInitialize]
    public void Init()
    {
        _orderTagRepository = new MongoDBRepositoryTest<OrderTag>();
        _orderRepository = new MongoDBRepositoryTest<Order>();

        _mediatorMock = new Mock<IMediator>();

        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });

        _service = new OrderTagService(_orderTagRepository, _orderRepository, _cacheBase, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task GetAllOrderTagsTest()
    {
        //Arrange
        await _orderTagRepository.InsertAsync(new OrderTag());
        await _orderTagRepository.InsertAsync(new OrderTag());
        await _orderTagRepository.InsertAsync(new OrderTag());

        //Act
        var result = await _service.GetAllOrderTags();

        //Assert
        Assert.AreEqual(3, result.Count);
    }

    [TestMethod]
    public async Task GetOrderTagByIdTest()
    {
        //Arrange
        await _orderTagRepository.InsertAsync(new OrderTag { Id = "1" });
        await _orderTagRepository.InsertAsync(new OrderTag());
        await _orderTagRepository.InsertAsync(new OrderTag());

        //Act
        var result = await _service.GetOrderTagById("1");

        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetOrderTagByNameTest()
    {
        //Arrange
        await _orderTagRepository.InsertAsync(new OrderTag { Id = "1", Name = "test" });
        await _orderTagRepository.InsertAsync(new OrderTag());
        await _orderTagRepository.InsertAsync(new OrderTag());

        //Act
        var result = await _service.GetOrderTagByName("test");

        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task InsertOrderTagTest()
    {
        //Act
        await _service.InsertOrderTag(new OrderTag());

        //Assert
        Assert.IsTrue(_orderTagRepository.Table.Any());
    }

    [TestMethod]
    public async Task UpdateOrderTagTest()
    {
        //Arrange
        var orderTag = new OrderTag();
        await _orderTagRepository.InsertAsync(orderTag);

        //Act
        orderTag.Name = "test";
        await _service.UpdateOrderTag(orderTag);

        //Assert
        Assert.IsTrue(_orderTagRepository.Table.FirstOrDefault(x => x.Id == orderTag.Id).Name == "test");
    }

    [TestMethod]
    public async Task DeleteOrderTagTest()
    {
        //Arrange
        var orderTag = new OrderTag();
        await _orderTagRepository.InsertAsync(orderTag);

        //Act
        await _service.DeleteOrderTag(orderTag);

        //Assert
        Assert.IsFalse(_orderTagRepository.Table.FirstOrDefault(x => x.Id == orderTag.Id)?.Name == "test");
    }

    [TestMethod]
    public async Task AttachOrderTagTest()
    {
        //Arrange
        var orderTag = new OrderTag();
        await _orderTagRepository.InsertAsync(orderTag);
        var order = new Order();
        await _orderRepository.InsertAsync(order);

        //Act
        await _service.AttachOrderTag(orderTag.Id, order.Id);

        //Assert
        Assert.IsTrue(_orderRepository.Table.FirstOrDefault(x => x.Id == order.Id).OrderTags
            .Any(z => z == orderTag.Id));
        Assert.IsTrue(_orderTagRepository.Table.FirstOrDefault(x => x.Id == orderTag.Id).Count == 1);
    }

    [TestMethod]
    public async Task DetachOrderTagTest()
    {
        //Arrange
        var orderTag = new OrderTag();
        await _orderTagRepository.InsertAsync(orderTag);
        var order = new Order();
        await _orderRepository.InsertAsync(order);

        await _service.AttachOrderTag(orderTag.Id, order.Id);

        //Act
        await _service.DetachOrderTag(orderTag.Id, order.Id);

        //Assert
        Assert.IsFalse(
            _orderRepository.Table.FirstOrDefault(x => x.Id == order.Id).OrderTags.Any(z => z == orderTag.Id));
        Assert.IsTrue(_orderTagRepository.Table.FirstOrDefault(x => x.Id == orderTag.Id).Count == 0);
    }

    [TestMethod]
    public async Task GetOrderCountTest()
    {
        //Arrange
        var orderTag = new OrderTag();
        await _orderTagRepository.InsertAsync(orderTag);
        var order = new Order();
        await _orderRepository.InsertAsync(order);

        await _service.AttachOrderTag(orderTag.Id, order.Id);

        //Act
        var result = await _service.GetOrderCount(orderTag.Id, string.Empty);

        //Assert
        Assert.AreEqual(1, result);
    }
}