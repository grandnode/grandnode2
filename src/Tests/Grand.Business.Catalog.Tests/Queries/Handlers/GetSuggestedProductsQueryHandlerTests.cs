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
public class GetSuggestedProductsQueryHandlerTests
{
    private Mock<ICacheBase> _casheManagerMock;
    private Mock<IRepository<CustomerTagProduct>> _customerGroupProductRepositoryMock;
    private Mock<IProductService> _productServiceMock;

    private GetSuggestedProductsQueryHandler handler;

    [TestInitialize]
    public void Init()
    {
        _casheManagerMock = new Mock<ICacheBase>();
        _productServiceMock = new Mock<IProductService>();
        _customerGroupProductRepositoryMock = new Mock<IRepository<CustomerTagProduct>>();

        handler = new GetSuggestedProductsQueryHandler(_productServiceMock.Object, _casheManagerMock.Object,
            _customerGroupProductRepositoryMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        var suggestedProductsQuery = new GetSuggestedProductsQuery {
            ProductsNumber = 10,
            Customer = new Customer()
        };
        await handler.Handle(suggestedProductsQuery, CancellationToken.None);
        _casheManagerMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<Product>>>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_WithoutCustomer_ThrowsBeforeCaching()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() =>
            handler.Handle(new GetSuggestedProductsQuery { ProductsNumber = 1 },
                CancellationToken.None));
        _casheManagerMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<Product>>>>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_KeysAndFiltersByTagsOfQueryCustomer()
    {
        //Arrange
        var customer = new Customer();
        customer.CustomerTags.Add("t1");
        _casheManagerMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<Product>>>>()))
            .Returns((string _, Func<Task<List<Product>>> acquire) => acquire());
        _customerGroupProductRepositoryMock.Setup(r => r.Table)
            .Returns(new List<CustomerTagProduct> {
                new() { CustomerTagId = "t1", ProductId = "p1" }, new() { CustomerTagId = "t2", ProductId = "p2" }
            }.AsQueryable());
        _productServiceMock.Setup(p => p.GetProductsByIds(It.IsAny<string[]>(), CatalogAccess.For(customer, "s1")))
            .ReturnsAsync(new List<Product>());

        //Act
        await handler.Handle(new GetSuggestedProductsQuery { Customer = customer, StoreId = "s1", ProductsNumber = 10 },
            CancellationToken.None);

        //Assert
        _casheManagerMock.Verify(c => c.GetAsync(string.Format(CacheKey.PRODUCTS_CUSTOMER_TAG, "t1"),
            It.IsAny<Func<Task<List<Product>>>>()), Times.Once);
        _productServiceMock.Verify(p => p.GetProductsByIds(new[] { "p1" },
            CatalogAccess.For(customer, "s1")), Times.Once);
    }
}