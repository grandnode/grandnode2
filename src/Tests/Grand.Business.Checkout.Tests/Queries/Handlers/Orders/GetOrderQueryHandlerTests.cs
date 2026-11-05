using Grand.Business.Checkout.Queries.Handlers.Orders;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data.Tests.LiteDb;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Checkout.Tests.Queries.Handlers.Orders;

[TestClass]
public class GetOrderQueryHandlerTests
{
    [TestMethod]
    public async Task Handle_ByBillingLastName_OnLiteDb_SkipsAddressesWithoutOne()
    {
        //Table on LiteDB is LINQ over objects, so a null here throws instead of not matching
        var repository = new LiteDBRepositoryMock<Order>();
        await repository.InsertAsync(new Order { Code = "NONAME", BillingAddress = new Address() });
        await repository.InsertAsync(new Order { Code = "NAMED", BillingAddress = new Address { LastName = "Kowalski" } });

        var result = (await new GetOrderQueryHandler(repository)
            .Handle(new GetOrderQuery { BillingLastName = "Kowal" }, CancellationToken.None)).ToList();

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("NAMED", result[0].Code);
    }
}
