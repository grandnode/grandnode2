using Grand.Business.Catalog.Events.Handlers;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Grand.Domain.Shipping;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Events.Handlers
{
    [TestClass()]
    public class DeliveryDateDeletedEventHandlerTests
    {
        private IRepository<Product> _repository;
        private DeliveryDateDeletedEventHandler _handler;

        [TestInitialize()]
        public void Init()
        {
            _repository = new MongoDBRepositoryTest<Product>();
            _handler = new DeliveryDateDeletedEventHandler(_repository);
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            //Arrange
            var deliveryDate = new DeliveryDate();
            var product = new Product();
            product.DeliveryDateId = deliveryDate.Id;
            await _repository.InsertAsync(product);
            var product2 = new Product();
            product2.DeliveryDateId = deliveryDate.Id;
            await _repository.InsertAsync(product2);
            var product3 = new Product();
            product3.DeliveryDateId = "1";
            await _repository.InsertAsync(product3);

            //Act
            await _handler.Handle(new Infrastructure.Events.EntityDeleted<DeliveryDate>(deliveryDate), CancellationToken.None);
            //Assert
            Assert.AreEqual(0, _repository.Table.Where(x => x.DeliveryDateId == deliveryDate.Id).Count());
            Assert.AreEqual(1, _repository.Table.Where(x => x.DeliveryDateId == "1").Count());
        }
    }
}