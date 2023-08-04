using Grand.Business.Catalog.Events.Handlers;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Grand.Domain.Shipping;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Events.Handlers
{
    [TestClass()]
    public class WarehouseDeletedEventHandlerTests
    {
        private IRepository<Product> _repository;
        private WarehouseDeletedEventHandler _handler;

        [TestInitialize()]
        public void Init()
        {
            _repository = new MongoDBRepositoryTest<Product>();
            _handler = new WarehouseDeletedEventHandler(_repository);
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            //Arrange
            var warehouse = new Warehouse();
            var product = new Product();
            product.WarehouseId = warehouse.Id;
            await _repository.InsertAsync(product);
            var product2 = new Product();
            product2.WarehouseId = warehouse.Id;
            await _repository.InsertAsync(product2);
            var product3 = new Product();
            product3.WarehouseId = "1";
            await _repository.InsertAsync(product3);

            //Act
            await _handler.Handle(new Infrastructure.Events.EntityDeleted<Warehouse>(warehouse), CancellationToken.None);
            //Assert
            Assert.AreEqual(0, _repository.Table.Where(x => x.WarehouseId == warehouse.Id).Count());
            Assert.AreEqual(1, _repository.Table.Where(x => x.WarehouseId == "1").Count());
        }
    }
}