using Grand.Business.Catalog.Events.Handlers;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Events.Handlers;

[TestClass]
public class DeliveryDateDeletedEventHandlerTests
{
    private DeliveryDateDeletedEventHandler _handler;
    private IRepository<Product> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Product>();
        _handler = new DeliveryDateDeletedEventHandler(_repository);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var deliveryDate = new DeliveryDate();
        var product = new Product {
            DeliveryDateId = deliveryDate.Id
        };
        await _repository.InsertAsync(product);
        var product2 = new Product {
            DeliveryDateId = deliveryDate.Id
        };
        await _repository.InsertAsync(product2);
        var product3 = new Product {
            DeliveryDateId = "1"
        };
        await _repository.InsertAsync(product3);

        //Act
        await _handler.Handle(new EntityDeleted<DeliveryDate>(deliveryDate), CancellationToken.None);
        //Assert
        Assert.AreEqual(0, _repository.Table.Where(x => x.DeliveryDateId == deliveryDate.Id).Count());
        Assert.AreEqual(1, _repository.Table.Where(x => x.DeliveryDateId == "1").Count());
    }
}