using Grand.Business.Catalog.Events.Handlers;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Events.Handlers
{
    [TestClass()]
    public class BrandDeletedEventHandlerTests
    {
        private IRepository<Product> _repository;
        private BrandDeletedEventHandler _handler;

        [TestInitialize()]
        public void Init()
        {
            _repository = new MongoDBRepositoryTest<Product>();
            _handler = new BrandDeletedEventHandler(_repository);
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            //Arrange
            var brand = new Brand();
            var product = new Product();
            product.BrandId = brand.Id;
            await _repository.InsertAsync(product);
            var product2 = new Product();
            product2.BrandId = brand.Id;
            await _repository.InsertAsync(product2);
            var product3 = new Product();
            product3.BrandId = "1";
            await _repository.InsertAsync(product3);

            //Act
            await _handler.Handle(new Infrastructure.Events.EntityDeleted<Brand>(brand), CancellationToken.None);
            //Assert
            Assert.AreEqual(0, _repository.Table.Where(x => x.BrandId == brand.Id).Count());
            Assert.AreEqual(1, _repository.Table.Where(x => x.BrandId == "1").Count());
        }
    }
}