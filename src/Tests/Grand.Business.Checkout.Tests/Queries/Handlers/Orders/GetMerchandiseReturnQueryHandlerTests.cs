using Grand.Business.Checkout.Queries.Handlers.Orders;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Checkout.Tests.Queries.Handlers.Orders;

[TestClass]
public class GetMerchandiseReturnQueryHandlerTests
{
    private MongoDBRepositoryTest<MerchandiseReturn> _repository;
    private GetMerchandiseReturnQueryHandler _handler;

    public GetMerchandiseReturnQueryHandlerTests()
    {
        _repository = new MongoDBRepositoryTest<MerchandiseReturn>();
        _handler = new GetMerchandiseReturnQueryHandler(_repository);
    }

    private async Task<MerchandiseReturn> Insert(Action<MerchandiseReturn>? setup = null)
    {
        var merchandiseReturn = new MerchandiseReturn { CreatedOnUtc = DateTime.UtcNow };
        setup?.Invoke(merchandiseReturn);
        var createdOnUtc = merchandiseReturn.CreatedOnUtc;
        await _repository.InsertAsync(merchandiseReturn);
        //the repository stamps the insert time: the test's own date is written after it
        await _repository.UpdateOneAsync(x => x.Id == merchandiseReturn.Id,
            UpdateBuilder<MerchandiseReturn>.Create().Set(x => x.CreatedOnUtc, createdOnUtc));
        return merchandiseReturn;
    }

    [TestMethod]
    public async Task SalesEmployeeId_OnlyTheirReturns()
    {
        var theirs = await Insert(x => x.SeId = "se-1");
        await Insert(x => x.SeId = "se-2");
        await Insert();

        var result = await _handler.Handle(new GetMerchandiseReturnQuery { SalesEmployeeId = "se-1" }, default);

        Assert.AreEqual(theirs.Id, result.Single().Id);
    }

    [TestMethod]
    public async Task ReturnShippingProvider_OnlyThatMethod()
    {
        var parcel = await Insert(x => x.ReturnShipping = new MerchandiseReturnShipping {
            ProviderSystemName = "ReturnShipping.Parcel"
        });
        await Insert(x => x.ReturnShipping = new MerchandiseReturnShipping {
            ProviderSystemName = "ReturnShipping.InStore"
        });
        await Insert();

        var result = await _handler.Handle(
            new GetMerchandiseReturnQuery { ReturnShippingProvider = "ReturnShipping.Parcel" }, default);

        Assert.AreEqual(parcel.Id, result.Single().Id);
    }

    [TestMethod]
    public async Task ReasonId_AnyItemWithThatReason()
    {
        var both = await Insert(x => {
            x.MerchandiseReturnItems.Add(new MerchandiseReturnItem { ReasonId = "r1" });
            x.MerchandiseReturnItems.Add(new MerchandiseReturnItem { ReasonId = "r2" });
        });
        await Insert(x => x.MerchandiseReturnItems.Add(new MerchandiseReturnItem { ReasonId = "r1" }));

        var result = await _handler.Handle(new GetMerchandiseReturnQuery { ReasonId = "r2" }, default);

        Assert.AreEqual(both.Id, result.Single().Id);
    }

    [TestMethod]
    public async Task OpenOnly_PendingAcceptedReceived()
    {
        foreach (var status in Enum.GetValues<MerchandiseReturnStatus>())
            await Insert(x => x.MerchandiseReturnStatus = status);

        var result = await _handler.Handle(new GetMerchandiseReturnQuery { OpenOnly = true }, default);

        CollectionAssert.AreEquivalent(
            new[] { MerchandiseReturnStatus.Pending, MerchandiseReturnStatus.Accepted, MerchandiseReturnStatus.Received },
            result.Select(x => x.MerchandiseReturnStatus).ToArray());
    }

    [TestMethod]
    public async Task OpenOnlyAndCreatedTo_OlderOpenOnly()
    {
        var old = await Insert(x => x.CreatedOnUtc = DateTime.UtcNow.AddDays(-20));
        await Insert(x => x.CreatedOnUtc = DateTime.UtcNow.AddDays(-2));
        await Insert(x => {
            x.CreatedOnUtc = DateTime.UtcNow.AddDays(-30);
            x.MerchandiseReturnStatus = MerchandiseReturnStatus.Completed;
        });

        var result = await _handler.Handle(new GetMerchandiseReturnQuery {
            OpenOnly = true, CreatedToUtc = DateTime.UtcNow.AddDays(-10)
        }, default);

        Assert.AreEqual(old.Id, result.Single().Id);
    }
}
