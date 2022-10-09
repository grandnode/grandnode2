﻿using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Infrastructure.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Queries.Handlers.Tests
{
    [TestClass()]
    public class GetPersonalizedProductsQueryHandlerTests
    {
        private Mock<ICacheBase> _casheManagerMock;
        private Mock<IProductService> _productServiceMock;
        private Mock<IRepository<CustomerProduct>> _customerProductRepositoryMock;

        private GetPersonalizedProductsQueryHandler handler;
        [TestInitialize()]
        public void Init()
        {
            _casheManagerMock = new Mock<ICacheBase>();
            _productServiceMock = new Mock<IProductService>();
            _customerProductRepositoryMock = new Mock<IRepository<CustomerProduct>>();

            handler = new GetPersonalizedProductsQueryHandler(_productServiceMock.Object, _casheManagerMock.Object, _customerProductRepositoryMock.Object);
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            await handler.Handle(new Core.Queries.Catalog.GetPersonalizedProductsQuery(), CancellationToken.None);
            _casheManagerMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<Product>>>>()), Times.Once);
        }
    }
}