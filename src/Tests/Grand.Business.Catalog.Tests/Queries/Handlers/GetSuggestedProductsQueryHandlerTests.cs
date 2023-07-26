using Grand.Business.Catalog.Queries.Handlers;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Infrastructure.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Queries.Handlers
{
    [TestClass()]
    public class GetSuggestedProductsQueryHandlerTests
    {
        private Mock<ICacheBase> _casheManagerMock;
        private Mock<IProductService> _productServiceMock;
        private Mock<IRepository<CustomerTagProduct>> _customerGroupProductRepositoryMock;

        private GetSuggestedProductsQueryHandler handler;

        [TestInitialize()]
        public void Init()
        {
            _casheManagerMock = new Mock<ICacheBase>();
            _productServiceMock = new Mock<IProductService>();
            _customerGroupProductRepositoryMock = new Mock<IRepository<CustomerTagProduct>>();

            handler = new GetSuggestedProductsQueryHandler(_productServiceMock.Object, _casheManagerMock.Object, _customerGroupProductRepositoryMock.Object);
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            var suggestedProductsQuery = new Core.Queries.Catalog.GetSuggestedProductsQuery();
            suggestedProductsQuery.CustomerTagIds = new[] { "1" };
            suggestedProductsQuery.ProductsNumber = 10;
            await handler.Handle(suggestedProductsQuery, CancellationToken.None);
            _casheManagerMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<Product>>>>()), Times.Once);
        }
    }
}