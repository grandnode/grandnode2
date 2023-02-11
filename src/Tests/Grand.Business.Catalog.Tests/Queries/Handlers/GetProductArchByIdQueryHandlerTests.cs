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
    public class GetProductArchByIdQueryHandlerTests
    {
        private Mock<IRepository<ProductDeleted>> _productDeletedRepository;

        private GetProductArchByIdQueryHandler handler;
        
        [TestInitialize()]
        public void Init()
        {
            _productDeletedRepository = new Mock<IRepository<ProductDeleted>>();

            handler = new GetProductArchByIdQueryHandler(_productDeletedRepository.Object);
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            //Act
            var result = await handler.Handle(new Core.Queries.Catalog.GetProductArchByIdQuery(), CancellationToken.None);
            //Assert
            Assert.IsNull(result);
        }
    }
}