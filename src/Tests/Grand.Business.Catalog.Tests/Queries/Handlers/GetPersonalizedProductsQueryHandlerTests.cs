using Grand.Business.Catalog.Queries.Handlers;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Queries.Catalog;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Queries.Handlers;

[TestClass]
public class GetPersonalizedProductsQueryHandlerTests
{
    private Mock<ICacheBase> _casheManagerMock;
    private Mock<IRepository<CustomerProduct>> _customerProductRepositoryMock;
    private Mock<IProductService> _productServiceMock;

    private GetPersonalizedProductsQueryHandler handler;

    [TestInitialize]
    public void Init()
    {
        _casheManagerMock = new Mock<ICacheBase>();
        _productServiceMock = new Mock<IProductService>();
        _customerProductRepositoryMock = new Mock<IRepository<CustomerProduct>>();

        handler = new GetPersonalizedProductsQueryHandler(_productServiceMock.Object, _casheManagerMock.Object,
            _customerProductRepositoryMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        await handler.Handle(new GetPersonalizedProductsQuery { Customer = new Customer() }, CancellationToken.None);
        _casheManagerMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<Product>>>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_WithoutCustomer_ThrowsBeforeCaching()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() =>
            handler.Handle(new GetPersonalizedProductsQuery { ProductsNumber = 1 },
                CancellationToken.None));
        _casheManagerMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<Product>>>>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_KeysAndLoadsByIdOfQueryCustomer()
    {
        //Arrange
        var customer = new Customer { Id = "c1" };
        _casheManagerMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<Product>>>>()))
            .Returns((string _, Func<Task<List<Product>>> acquire) => acquire());
        _customerProductRepositoryMock.Setup(r => r.Table)
            .Returns(new List<CustomerProduct> {
                new() { CustomerId = "c1", ProductId = "p1" }, new() { CustomerId = "c2", ProductId = "p2" }
            }.AsQueryable());
        _productServiceMock.Setup(p => p.GetProductsByIds(It.IsAny<string[]>(), CatalogAccess.For(customer, "s1")))
            .ReturnsAsync(new List<Product>());

        //Act
        await handler.Handle(new GetPersonalizedProductsQuery { Customer = customer, StoreId = "s1", ProductsNumber = 10 },
            CancellationToken.None);

        //Assert
        _casheManagerMock.Verify(c => c.GetAsync(string.Format(CacheKey.PRODUCTS_CUSTOMER_PERSONAL_KEY, "c1"),
            It.IsAny<Func<Task<List<Product>>>>()), Times.Once);
        _productServiceMock.Verify(p => p.GetProductsByIds(new[] { "p1" },
            CatalogAccess.For(customer, "s1")), Times.Once);
    }
}