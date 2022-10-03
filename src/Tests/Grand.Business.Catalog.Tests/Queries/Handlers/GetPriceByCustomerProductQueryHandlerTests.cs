using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Infrastructure.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace Grand.Business.Catalog.Queries.Handlers.Tests
{
    [TestClass()]
    public class GetPriceByCustomerProductQueryHandlerTests
    {
        private Mock<ICacheBase> _casheManagerMock;
        private Mock<IRepository<CustomerProductPrice>> _customerProductPriceRepositoryMock;

        private GetPriceByCustomerProductQueryHandler handler;
        
        [TestInitialize()]
        public void Init()
        {
            _casheManagerMock = new Mock<ICacheBase>();
            _customerProductPriceRepositoryMock = new Mock<IRepository<CustomerProductPrice>>();

            handler = new GetPriceByCustomerProductQueryHandler(_casheManagerMock.Object, _customerProductPriceRepositoryMock.Object);
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            await handler.Handle(new Core.Queries.Catalog.GetPriceByCustomerProductQuery(), CancellationToken.None);
            _casheManagerMock.Verify(c => c.Get(It.IsAny<string>(), It.IsAny<Func<(CustomerProductPrice, bool)>>()), Times.Once);
        }
    }
}