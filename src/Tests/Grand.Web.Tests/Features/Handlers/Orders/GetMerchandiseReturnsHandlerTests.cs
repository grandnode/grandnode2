using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Mediator;
using Grand.Web.Common.Localization;
using Grand.Web.Features.Handlers.Orders;
using Grand.Web.Features.Models.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Features.Handlers.Orders;

[TestClass]
public class GetMerchandiseReturnsHandlerTests
{
    [TestMethod]
    public async Task GetMerchandiseReturnsHandler_OrderId_QueriesByOrder()
    {
        //Arrange - a guest's grant lists the returns of its order, not those of a customer
        GetMerchandiseReturnQuery query = null;
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.IsAny<GetMerchandiseReturnQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<IPagedList<MerchandiseReturn>>, CancellationToken>((q, _) =>
                query = (GetMerchandiseReturnQuery)q)
            .ReturnsAsync(new PagedList<MerchandiseReturn>([], 0, 10));
        var groupService = new Mock<IGroupService>();
        groupService.Setup(x => x.IsOwner(It.IsAny<Customer>())).ReturnsAsync(true);
        var handler = new GetMerchandiseReturnsHandler(new Mock<IOrderService>().Object,
            new Mock<IDateTimeService>().Object, new Mock<IPriceFormatter>().Object, groupService.Object,
            mediator.Object, new Mock<IEnumTranslationService>().Object);

        //Act
        await handler.Handle(new GetMerchandiseReturns {
            Customer = new Customer { Id = "g1" }, OrderId = "o1", Store = new Domain.Stores.Store { Id = "s1" }
        }, CancellationToken.None);

        //Assert
        Assert.AreEqual("o1", query.OrderId);
        Assert.AreEqual("s1", query.StoreId);
        Assert.AreEqual("", query.CustomerId);
        Assert.AreEqual("", query.OwnerId);
    }
}
