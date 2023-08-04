using Grand.Business.Catalog.Events.Handlers;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Grand.Domain.Directory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Events.Handlers
{
    [TestClass()]
    public class DeleteMeasureUnitOnProductEventHandlerTests
    {
        private IRepository<Product> _repository;
        private DeleteMeasureUnitOnProductEventHandler _handler;

        [TestInitialize()]
        public void Init()
        {
            _repository = new MongoDBRepositoryTest<Product>();
            _handler = new DeleteMeasureUnitOnProductEventHandler(_repository);
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            //Arrange
            var measureUnit = new MeasureUnit();
            var product = new Product();
            product.UnitId = measureUnit.Id;
            await _repository.InsertAsync(product);
            var product2 = new Product();
            product2.UnitId = measureUnit.Id;
            await _repository.InsertAsync(product2);
            var product3 = new Product();
            product3.UnitId = "1";
            await _repository.InsertAsync(product3);

            //Act
            await _handler.Handle(new Infrastructure.Events.EntityDeleted<MeasureUnit>(measureUnit), CancellationToken.None);
            //Assert
            Assert.AreEqual(0, _repository.Table.Where(x => x.UnitId == measureUnit.Id).Count());
            Assert.AreEqual(1, _repository.Table.Where(x => x.UnitId == "1").Count());
        }
    }
}