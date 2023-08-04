using Grand.Business.Catalog.Events.Handlers;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Events.Handlers
{
    [TestClass()]
    public class UpdateProductOnCartEventHandlerTests
    {
        private IRepository<Customer> _repository;
        private UpdateProductOnCartEventHandler _handler;

        [TestInitialize()]
        public void Init()
        {
            _repository = new MongoDBRepositoryTest<Customer>();
            _handler = new UpdateProductOnCartEventHandler(_repository);
        }



        [TestMethod()]
        public async Task HandleTest()
        {
            //Arrange
            var product = new Product();
            var customer = new Customer();
            customer.ShoppingCartItems.Add(new Domain.Orders.ShoppingCartItem() 
            { 
                ProductId = product.Id,
                IsFreeShipping = !product.IsFreeShipping,
                IsShipEnabled = !product.IsShipEnabled,
                IsTaxExempt = !product.IsTaxExempt,
                IsGiftVoucher = !product.IsGiftVoucher
            });
            //Act
            await _handler.Handle(new Core.Events.Catalog.UpdateProductOnCartEvent(product), CancellationToken.None);

            //Assert
            Assert.AreEqual(0, _repository.Table.Where(x => x.ShoppingCartItems.Any(y => y.IsTaxExempt == product.IsTaxExempt)).Count());
            Assert.AreEqual(0, _repository.Table.Where(x => x.ShoppingCartItems.Any(y => y.IsFreeShipping == product.IsFreeShipping)).Count());
            Assert.AreEqual(0, _repository.Table.Where(x => x.ShoppingCartItems.Any(y => y.IsFreeShipping == product.IsFreeShipping)).Count());
            Assert.AreEqual(0, _repository.Table.Where(x => x.ShoppingCartItems.Any(y => y.IsGiftVoucher == product.IsGiftVoucher)).Count());

        }
    }
}