using Grand.Business.Catalog.Queries.Handlers;
using Grand.Business.Core.Queries.Catalog;
using Grand.Data;
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
        _casheManagerMock.Verify(c => c.Get(It.IsAny<string>(), It.IsAny<Func<(CustomerProductPrice, bool)>>()),
            Times.Once);
    }
}