using Grand.Business.Catalog.Queries.Handlers;
using Grand.Business.Core.Queries.Catalog;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Queries.Handlers;

[TestClass]
public class GetPriceByCustomerProductQueryHandlerTests
{
    private Mock<ICacheBase> _casheManagerMock;
    private Mock<IRepository<CustomerProductPrice>> _customerProductPriceRepositoryMock;

    private GetPriceByCustomerProductQueryHandler handler;

    [TestInitialize]
    public void Init()
    {
        _casheManagerMock = new Mock<ICacheBase>();
        _customerProductPriceRepositoryMock = new Mock<IRepository<CustomerProductPrice>>();

        handler = new GetPriceByCustomerProductQueryHandler(_casheManagerMock.Object,
            _customerProductPriceRepositoryMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        await handler.Handle(new GetPriceByCustomerProductQuery(), CancellationToken.None);
        _casheManagerMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<CustomerProductPrice>>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_OnCacheMiss_ReadsThePriceOfThatCustomerAndProduct()
    {
        //Arrange
        var repository = new MongoDBRepositoryTest<CustomerProductPrice>();
        await repository.InsertAsync(new CustomerProductPrice { CustomerId = "c1", ProductId = "p1", Price = 5 });
        await repository.InsertAsync(new CustomerProductPrice { CustomerId = "c1", ProductId = "p2", Price = 7 });
        await repository.InsertAsync(new CustomerProductPrice { CustomerId = "c2", ProductId = "p1", Price = 9 });
        _casheManagerMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<CustomerProductPrice>>>()))
            .Returns((string _, Func<Task<CustomerProductPrice>> acquire) => acquire());
        var readingHandler = new GetPriceByCustomerProductQueryHandler(_casheManagerMock.Object, repository);
        //Act
        var price = await readingHandler.Handle(
            new GetPriceByCustomerProductQuery { CustomerId = "c1", ProductId = "p1" }, CancellationToken.None);
        //Assert
        Assert.AreEqual(5, price);
    }
}