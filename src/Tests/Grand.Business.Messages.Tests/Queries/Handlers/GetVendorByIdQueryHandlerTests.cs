using Grand.Business.Core.Queries.Messages;
using Grand.Business.Messages.Queries.Handlers;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Vendors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Messages.Tests.Queries.Handlers;

[TestClass]
public class GetVendorByIdQueryHandlerTests
{
    private IRepository<Vendor> _repository;
    private GetVendorByIdQueryHandler handler;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Vendor>();
        handler = new GetVendorByIdQueryHandler(_repository);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        await _repository.InsertAsync(new Vendor { Id = "1" });
        //Act
        var result = await handler.Handle(new GetVendorByIdQuery { Id = "1" }, CancellationToken.None);
        //Arrange
        Assert.IsNotNull(result);
    }
}