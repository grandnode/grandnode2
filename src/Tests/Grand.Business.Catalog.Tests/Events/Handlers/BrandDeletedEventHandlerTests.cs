using Grand.Business.Catalog.Events.Handlers;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Events.Handlers;

[TestClass]
public class BrandDeletedEventHandlerTests
{
    private BrandDeletedEventHandler _handler;
    private IRepository<Product> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Product>();
        _handler = new BrandDeletedEventHandler(_repository);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var brand = new Brand();
        var product = new Product {
            BrandId = brand.Id
        };
        await _repository.InsertAsync(product);
        var product2 = new Product {
            BrandId = brand.Id
        };
        await _repository.InsertAsync(product2);
        var product3 = new Product {
            BrandId = "1"
        };
        await _repository.InsertAsync(product3);

        //Act
        await _handler.Handle(new EntityDeleted<Brand>(brand), CancellationToken.None);
        //Assert
        Assert.AreEqual(0, _repository.Table.Where(x => x.BrandId == brand.Id).Count());
        Assert.AreEqual(1, _repository.Table.Where(x => x.BrandId == "1").Count());
    }
}