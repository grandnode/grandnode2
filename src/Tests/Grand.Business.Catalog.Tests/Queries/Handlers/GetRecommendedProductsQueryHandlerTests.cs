using Grand.Business.Catalog.Queries.Handlers;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Queries.Catalog;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Queries.Handlers;

[TestClass]
public class GetRecommendedProductsQueryHandlerTests
{
    private Mock<ICacheBase> _casheManagerMock;
    private Mock<IRepository<CustomerGroupProduct>> _customerGroupProductRepositoryMock;
    private Mock<IProductService> _productServiceMock;

    private GetRecommendedProductsQueryHandler handler;

    [TestInitialize]
    public void Init()
    {
        _casheManagerMock = new Mock<ICacheBase>();
        _productServiceMock = new Mock<IProductService>();
        _customerGroupProductRepositoryMock = new Mock<IRepository<CustomerGroupProduct>>();

        handler = new GetRecommendedProductsQueryHandler(_productServiceMock.Object, _casheManagerMock.Object,
            _customerGroupProductRepositoryMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        var getRecommendedProductsQuery = new GetRecommendedProductsQuery {
            CustomerGroupIds = ["1"],
            StoreId = "1"
        };
        await handler.Handle(getRecommendedProductsQuery, CancellationToken.None);
        _casheManagerMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<Product>>>>()),
            Times.Once);
    }
}