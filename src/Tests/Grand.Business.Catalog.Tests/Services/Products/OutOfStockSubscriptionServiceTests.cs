using Grand.Business.Catalog.Services.Products;
using Grand.Business.Core.Commands.Catalog;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.SharedKernel.Extensions;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Products;

[TestClass]
public class OutOfStockSubscriptionServiceTests
{
    private Mock<IMediator> _mediatorMock;
    private OutOfStockSubscriptionService _outOfStockSubscriptionService;
    private IRepository<OutOfStockSubscription> _repository;

    [TestInitialize]
    public void InitializeTests()
    {
        CommonPath.BaseDirectory = "";

        _repository = new MongoDBRepositoryTest<OutOfStockSubscription>();
        _mediatorMock = new Mock<IMediator>();
        _mediatorMock.Setup(x => x.Send(It.IsAny<SendNotificationsToSubscribersCommand>(), default))
            .Returns(Task.FromResult((IList<OutOfStockSubscription>)new List<OutOfStockSubscription>()));

        _outOfStockSubscriptionService = new OutOfStockSubscriptionService(_repository, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task GetAllSubscriptionsByCustomerIdTest()
    {
        //Arange
        var outOfStockSubscription1 = new OutOfStockSubscription {
            CustomerId = "1"
        };
        var outOfStockSubscription2 = new OutOfStockSubscription {
            CustomerId = "1"
        };
        var outOfStockSubscription3 = new OutOfStockSubscription {
            CustomerId = "2"
        };
        await _repository.InsertAsync(outOfStockSubscription1);
        await _repository.InsertAsync(outOfStockSubscription2);
        await _repository.InsertAsync(outOfStockSubscription3);
        //Act
        var result = await _outOfStockSubscriptionService.GetAllSubscriptionsByCustomerId("1");

        //Assert
        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public async Task FindSubscriptionTest_IsNotNull()
    {
        //Arange
        var outOfStockSubscription1 = new OutOfStockSubscription {
            CustomerId = "1",
            ProductId = "1",
            StoreId = "",
            WarehouseId = ""
        };
        var outOfStockSubscription2 = new OutOfStockSubscription {
            CustomerId = "1",
            ProductId = "2",
            StoreId = "",
            WarehouseId = ""
        };
        var outOfStockSubscription3 = new OutOfStockSubscription {
            CustomerId = "2",
            ProductId = "3",
            StoreId = "",
            WarehouseId = ""
        };
        await _repository.InsertAsync(outOfStockSubscription1);
        await _repository.InsertAsync(outOfStockSubscription2);
        await _repository.InsertAsync(outOfStockSubscription3);

        //Act
        var result =
            await _outOfStockSubscriptionService.FindSubscription("1", "1", new List<CustomAttribute>(), "", "");

        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task FindSubscriptionTest_IsNull()
    {
        //Arange
        var outOfStockSubscription1 = new OutOfStockSubscription {
            CustomerId = "1",
            ProductId = "1",
            StoreId = "",
            WarehouseId = ""
        };
        var outOfStockSubscription2 = new OutOfStockSubscription {
            CustomerId = "1",
            ProductId = "2",
            StoreId = "",
            WarehouseId = ""
        };
        var outOfStockSubscription3 = new OutOfStockSubscription {
            CustomerId = "2",
            ProductId = "3",
            StoreId = "",
            WarehouseId = ""
        };
        await _repository.InsertAsync(outOfStockSubscription1);
        await _repository.InsertAsync(outOfStockSubscription2);
        await _repository.InsertAsync(outOfStockSubscription3);

        //Act
        var result =
            await _outOfStockSubscriptionService.FindSubscription("1", "1", new List<CustomAttribute>(), "1", "");

        //Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetSubscriptionByIdTest()
    {
        //Arange
        var outOfStockSubscription1 = new OutOfStockSubscription {
            CustomerId = "1",
            ProductId = "1",
            StoreId = "",
            WarehouseId = ""
        };
        var outOfStockSubscription2 = new OutOfStockSubscription {
            CustomerId = "1",
            ProductId = "2",
            StoreId = "",
            WarehouseId = ""
        };
        var outOfStockSubscription3 = new OutOfStockSubscription {
            CustomerId = "2",
            ProductId = "3",
            StoreId = "",
            WarehouseId = ""
        };
        await _repository.InsertAsync(outOfStockSubscription1);
        await _repository.InsertAsync(outOfStockSubscription2);
        await _repository.InsertAsync(outOfStockSubscription3);

        //Act
        var result = await _outOfStockSubscriptionService.GetSubscriptionById(outOfStockSubscription1.Id);

        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task InsertSubscriptionTest()
    {
        //Arange
        var outOfStockSubscription1 = new OutOfStockSubscription {
            CustomerId = "1",
            ProductId = "1",
            StoreId = "",
            WarehouseId = ""
        };
        //Act
        await _outOfStockSubscriptionService.InsertSubscription(outOfStockSubscription1);

        //Assert
        Assert.IsTrue(_repository.Table.Any());
        Assert.IsTrue(_repository.Table.Count() == 1);
    }

    [TestMethod]
    public async Task UpdateSubscriptionTest()
    {
        //Arange
        var outOfStockSubscription1 = new OutOfStockSubscription {
            CustomerId = "1",
            ProductId = "1",
            StoreId = "",
            WarehouseId = ""
        };
        await _outOfStockSubscriptionService.InsertSubscription(outOfStockSubscription1);
        outOfStockSubscription1.CustomerId = "2";
        //Act
        await _outOfStockSubscriptionService.UpdateSubscription(outOfStockSubscription1);
        //Assert
        Assert.IsTrue(_repository.Table.FirstOrDefault().CustomerId == "2");
    }

    [TestMethod]
    public async Task DeleteSubscriptionTest()
    {
        //Arange
        var outOfStockSubscription1 = new OutOfStockSubscription {
            CustomerId = "1",
            ProductId = "1",
            StoreId = "",
            WarehouseId = ""
        };
        await _outOfStockSubscriptionService.InsertSubscription(outOfStockSubscription1);
        //Act
        await _outOfStockSubscriptionService.DeleteSubscription(outOfStockSubscription1);

        //Assert
        Assert.IsFalse(_repository.Table.Any());
        Assert.IsTrue(_repository.Table.Count() == 0);
    }

    [TestMethod]
    public async Task SendNotificationsToSubscribersTest()
    {
        //Arange
        var outOfStockSubscription1 = new OutOfStockSubscription {
            CustomerId = "1",
            ProductId = "1",
            StoreId = "",
            WarehouseId = ""
        };
        var outOfStockSubscription2 = new OutOfStockSubscription {
            CustomerId = "1",
            ProductId = "2",
            StoreId = "",
            WarehouseId = ""
        };
        var outOfStockSubscription3 = new OutOfStockSubscription {
            CustomerId = "2",
            ProductId = "1",
            StoreId = "",
            WarehouseId = ""
        };

        await _repository.InsertAsync(outOfStockSubscription1);
        await _repository.InsertAsync(outOfStockSubscription2);
        await _repository.InsertAsync(outOfStockSubscription3);

        //Act
        await _outOfStockSubscriptionService.SendNotificationsToSubscribers(new Product { Id = "1" }, "");

        //Assert
        _mediatorMock.Verify(c => c.Send(It.IsAny<SendNotificationsToSubscribersCommand>(), default),
            Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task SendNotificationsToSubscribersTest1()
    {
        //Arange
        var outOfStockSubscription1 = new OutOfStockSubscription {
            CustomerId = "1",
            ProductId = "1",
            StoreId = "",
            WarehouseId = ""
        };
        outOfStockSubscription1.Attributes.Add(new CustomAttribute { Key = "MyKey", Value = "1" });

        var outOfStockSubscription2 = new OutOfStockSubscription {
            CustomerId = "1",
            ProductId = "2",
            StoreId = "",
            WarehouseId = ""
        };
        var outOfStockSubscription3 = new OutOfStockSubscription {
            CustomerId = "2",
            ProductId = "1",
            StoreId = "",
            WarehouseId = ""
        };
        await _repository.InsertAsync(outOfStockSubscription1);
        await _repository.InsertAsync(outOfStockSubscription2);
        await _repository.InsertAsync(outOfStockSubscription3);

        //Act
        await _outOfStockSubscriptionService.SendNotificationsToSubscribers(new Product { Id = "1" },
            new CustomAttribute[] { new() { Key = "MyKey", Value = "1" } }, "");

        //Assert
        _mediatorMock.Verify(c => c.Send(It.IsAny<SendNotificationsToSubscribersCommand>(), default),
            Times.AtLeastOnce);
    }
}