using Grand.Business.Catalog.Events.Handlers;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Tax;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Events.Handlers;

[TestClass]
public class TaxCategoryDeletedEventHandlerTests
{
    private TaxCategoryDeletedEventHandler _handler;
    private IRepository<Product> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Product>();
        _handler = new TaxCategoryDeletedEventHandler(_repository);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var taxCategory = new TaxCategory();
        var product = new Product {
            TaxCategoryId = taxCategory.Id
        };
        await _repository.InsertAsync(product);
        var product2 = new Product {
            TaxCategoryId = taxCategory.Id
        };
        await _repository.InsertAsync(product2);
        var product3 = new Product {
            TaxCategoryId = "1"
        };
        await _repository.InsertAsync(product3);

        //Act
        await _handler.Handle(new EntityDeleted<TaxCategory>(taxCategory), CancellationToken.None);
        //Assert
        Assert.AreEqual(0, _repository.Table.Where(x => x.TaxCategoryId == taxCategory.Id).Count());
        Assert.AreEqual(1, _repository.Table.Where(x => x.TaxCategoryId == "1").Count());
    }
}