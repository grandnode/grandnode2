using Grand.Business.Catalog.Events.Handlers;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Directory;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Events.Handlers;

[TestClass]
public class DeleteMeasureUnitOnProductEventHandlerTests
{
    private DeleteMeasureUnitOnProductEventHandler _handler;
    private IRepository<Product> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Product>();
        _handler = new DeleteMeasureUnitOnProductEventHandler(_repository);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var measureUnit = new MeasureUnit();
        var product = new Product {
            UnitId = measureUnit.Id
        };
        await _repository.InsertAsync(product);
        var product2 = new Product {
            UnitId = measureUnit.Id
        };
        await _repository.InsertAsync(product2);
        var product3 = new Product {
            UnitId = "1"
        };
        await _repository.InsertAsync(product3);

        //Act
        await _handler.Handle(new EntityDeleted<MeasureUnit>(measureUnit), CancellationToken.None);
        //Assert
        Assert.AreEqual(0, _repository.Table.Where(x => x.UnitId == measureUnit.Id).Count());
        Assert.AreEqual(1, _repository.Table.Where(x => x.UnitId == "1").Count());
    }
}