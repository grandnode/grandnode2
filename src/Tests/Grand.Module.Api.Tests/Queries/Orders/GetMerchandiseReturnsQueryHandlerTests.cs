using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain;
using Grand.Domain.Orders;
using Grand.Module.Api.Queries.Handlers.Orders;
using Grand.Module.Api.Queries.Models.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Module.Api.Tests.Queries.Orders;

[TestClass]
public class GetMerchandiseReturnsQueryHandlerTests
{
    private Mock<IMerchandiseReturnService> _merchandiseReturnServiceMock;

    [TestInitialize]
    public void Init()
    {
        _merchandiseReturnServiceMock = new Mock<IMerchandiseReturnService>();
    }

    private GetMerchandiseReturnsQueryHandler CreateHandler() => new(_merchandiseReturnServiceMock.Object);

    [TestMethod]
    public async Task Handle_ForwardsAllFiltersToSearch()
    {
        _merchandiseReturnServiceMock.Setup(s => s.SearchMerchandiseReturns(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<MerchandiseReturnStatus?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(new PagedList<MerchandiseReturn>());

        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);

        await CreateHandler().Handle(new GetMerchandiseReturnsQuery {
            StoreId = "s", CustomerId = "c", OrderItemId = "i", VendorId = "v", OwnerId = "o", OrderId = "ord",
            Status = MerchandiseReturnStatus.Accepted, ReturnShippingProvider = "ship", ReasonId = "reason",
            OpenOnly = true, CreatedFromUtc = from, CreatedToUtc = to, PageIndex = 2, PageSize = 50
        }, default);

        _merchandiseReturnServiceMock.Verify(s => s.SearchMerchandiseReturns(
            "s", "c", "i", "v", "o", MerchandiseReturnStatus.Accepted, 2, 50, from, to, "ord", "ship", "reason",
            true), Times.Once);
    }
}
