using Grand.Business.Catalog.Queries.Handlers;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Queries.Handlers
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