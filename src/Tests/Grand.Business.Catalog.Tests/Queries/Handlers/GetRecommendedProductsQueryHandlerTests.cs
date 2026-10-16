using Grand.Business.Catalog.Queries.Handlers;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Queries.Catalog;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
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
            StoreId = "1",
            Customer = new Customer()
        };
        await handler.Handle(getRecommendedProductsQuery, CancellationToken.None);
        _casheManagerMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<Product>>>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_WithoutCustomer_ThrowsBeforeCaching()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() =>
            handler.Handle(new GetRecommendedProductsQuery { StoreId = "1" },
                CancellationToken.None));
        _casheManagerMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<Product>>>>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_AuthorizesProductsAgainstCustomerAndStoreOfQuery()
    {
        //Arrange
        var customer = new Customer();
        customer.Groups.Add("1");
        _casheManagerMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<Product>>>>()))
            .Returns((string _, Func<Task<List<Product>>> acquire) => acquire());
        _customerGroupProductRepositoryMock.Setup(r => r.Table)
            .Returns(new List<CustomerGroupProduct> { new() { CustomerGroupId = "1", ProductId = "p1" } }
                .AsQueryable());
        _productServiceMock.Setup(p => p.GetProductsByIds(new[] { "p1" }, customer, "1", false))
            .ReturnsAsync(new List<Product>());

        //Act
        await handler.Handle(new GetRecommendedProductsQuery {
            StoreId = "1", Customer = customer
        }, CancellationToken.None);

        //Assert
        _casheManagerMock.Verify(c => c.GetAsync(string.Format(CacheKey.PRODUCTS_CUSTOMER_GROUP, "1", "1"), It.IsAny<Func<Task<List<Product>>>>()),
            Times.Once);
        _productServiceMock.Verify(p => p.GetProductsByIds(new[] { "p1" }, customer, "1", false), Times.Once);
    }
}